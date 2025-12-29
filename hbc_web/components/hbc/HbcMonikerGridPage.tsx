"use client"

import type {
  CellDoubleClickedEvent,
  ColDef,
  FilterChangedEvent,
  FilterModel,
  GridApi,
  GridReadyEvent,
  SelectionChangedEvent,
} from "ag-grid-community"
import { useEffect, useMemo, useRef, useState } from "react"

import { HbcAgTable } from "@/components/hbc/HbcAgTable"
import { HbcHelpTooltip } from "@/components/hbc/HbcHelpTooltip"
import { dateOnlyFromText, normalizeIsoishDateTime } from "@/components/hbc/dateTime"

const DEFAULT_PAGE_SIZE = 50
const SERVER_CONTAINS_MIN_CHARS = 3
const BACKEND_FILTER_DEBOUNCE_MS = 900

type ParsedRows<T> = {
  rows: T[]
  invalidCount: number
  issues: string[]
}

type PageResult<T> = ParsedRows<T> & {
  totalCount?: number
  error?: string
}

export type HbcMonikerGridPageProps<T extends Record<string, unknown>> = {
  title: string
  table: string
  docUrl: string
  rowIdField: Extract<keyof T, string>
  buildColumnDefs: (disableClientFilteringRef: React.MutableRefObject<boolean>) => ColDef<T>[]
  parseRows: (payload: unknown) => ParsedRows<T>
  pageSize?: number
  helpItems?: string[]
  dateFields?: string[]
  dateTimeFields?: string[]
}

function escapeOdataString(value: string) {
  return value.replace(/'/g, "''")
}

function pushDayRange(parts: string[], field: string, dateOnly: string) {
  const start = `${dateOnly}T00:00:00`
  const base = Date.parse(`${dateOnly}T00:00:00Z`)
  if (Number.isFinite(base)) {
    const next = new Date(base + 24 * 60 * 60 * 1000)
    const yyyy = String(next.getUTCFullYear()).padStart(4, "0")
    const mm = String(next.getUTCMonth() + 1).padStart(2, "0")
    const dd = String(next.getUTCDate()).padStart(2, "0")
    const end = `${yyyy}-${mm}-${dd}T00:00:00`
    parts.push(`(${field} ge '${start}' and ${field} lt '${end}')`)
  } else {
    parts.push(`${field} eq '${start}'`)
  }
}

function filterModelToOData(
  model: FilterModel | null | undefined,
  dateFields?: Set<string>,
  dateTimeFields?: Set<string>
) {
  if (!model) return undefined

  const parts: string[] = []

  for (const [field, raw] of Object.entries(model)) {
    const f = String(field)
    const m: any = raw
    if (!m) continue

    const type = String(m.type || "").toLowerCase()
    if (type && type !== "equals" && type !== "contains") continue

    if (typeof m.filter === "string") {
      const rawValue = m.filter.trim()
      if (type === "contains" && rawValue.length < SERVER_CONTAINS_MIN_CHARS) continue

      const v = escapeOdataString(rawValue)
      if (!v) continue

      if (type === "equals" && (dateFields?.has(f) || dateTimeFields?.has(f))) {
        const maybeDateTime = normalizeIsoishDateTime(rawValue)
        if (maybeDateTime && dateTimeFields?.has(f)) {
          parts.push(`${f} eq '${escapeOdataString(maybeDateTime)}'`)
          continue
        }

        const dateOnly = dateOnlyFromText(rawValue)
        if (dateOnly) {
          pushDayRange(parts, f, dateOnly)
        }
        continue
      }

      if (type === "contains") parts.push(`contains(${f},'${v}')`)
      else parts.push(`${f} eq '${v}'`)
      continue
    }

    if (typeof m.filter === "number" && Number.isFinite(m.filter)) {
      parts.push(`${f} eq ${m.filter}`)
      continue
    }

    if (typeof m.dateFrom === "string" && m.dateFrom.trim()) {
      const raw = m.dateFrom.trim()
      const dateOnlyMatch = /^(\d{4}-\d{2}-\d{2})/.exec(raw)
      const dateOnly = dateOnlyMatch ? dateOnlyMatch[1] : raw

      pushDayRange(parts, f, dateOnly)
      continue
    }
  }

  return parts.length ? parts.join(" and ") : undefined
}

function ExternalLinkIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M14 5h5v5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M10 14L19 5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M19 14v5H5V5h5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function HbcMonikerGridPage<T extends Record<string, unknown>>({
  title,
  table,
  docUrl,
  rowIdField,
  buildColumnDefs,
  parseRows,
  pageSize = DEFAULT_PAGE_SIZE,
  dateFields,
  dateTimeFields,
  helpItems = [
    "Single click focuses a cell; copy with Cmd+C (macOS) or Ctrl+C (Windows/Linux).",
    "Double click a cell to toggle selecting its entire row.",
    "Use the X button to clear any selected rows (de-select all).",
    "Use the filter-reset button to clear all column filters.",
    "Pagination buttons move between pages (first/prev/next/last); filters affect server-side results and paging.",
  ],
}: HbcMonikerGridPageProps<T>) {
  const [currentPage, setCurrentPage] = useState(1)
  const [filterModel, setFilterModel] = useState<FilterModel>({})
  const [filterOData, setFilterOData] = useState<string | undefined>(undefined)
  const [filterLabel, setFilterLabel] = useState<string | undefined>(undefined)
  const filterDebounceRef = useRef<number | null>(null)
  const disableClientFilteringRef = useRef(false)
  const dateFieldSet = useMemo(() => new Set((dateFields ?? []).map((x) => x.trim()).filter(Boolean)), [
    dateFields,
  ])
  const dateTimeFieldSet = useMemo(
    () => new Set((dateTimeFields ?? []).map((x) => x.trim()).filter(Boolean)),
    [dateTimeFields]
  )

  const gridApiRef = useRef<GridApi<T> | null>(null)
  const [selectedCount, setSelectedCount] = useState(0)

  const columnDefs = useMemo<ColDef<T>[]>(() => buildColumnDefs(disableClientFilteringRef), [
    buildColumnDefs,
  ])

  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<PageResult<T>>({
    rows: [],
    invalidCount: 0,
    issues: [],
  })

  useEffect(() => {
    const controller = new AbortController()

    async function run() {
      const apiBase = "/backend"
      const skip = (currentPage - 1) * pageSize
      const params = new URLSearchParams()
      params.set("$top", String(pageSize))
      params.set("$skip", String(skip))
      params.set("$count", "true")
      if (filterOData) params.set("$filter", filterOData)
      const url = `${apiBase}/${table}?${params.toString()}`

      setLoading(true)
      try {
        const res = await fetch(url, { cache: "no-store", signal: controller.signal })
        if (!res.ok) {
          setResult({
            rows: [],
            invalidCount: 0,
            issues: [],
            error: `REST API error ${res.status} on GET ${url}`,
          })
          return
        }

        const json = await res.json()

        const hasValue = typeof json === "object" && json !== null && "value" in (json as any)
        const payload = hasValue ? (json as any).value : json
        const parsed = parseRows(payload)

        const totalCount =
          typeof json === "object" &&
          json !== null &&
          "count" in (json as any) &&
          typeof (json as any).count === "number"
            ? ((json as any).count as number)
            : undefined

        setResult({ ...parsed, totalCount })
      } catch (e) {
        if (controller.signal.aborted) return
        setResult({
          rows: [],
          invalidCount: 0,
          issues: [],
          error: `Failed to fetch: ${e instanceof Error ? e.message : String(e)}`,
        })
      } finally {
        if (!controller.signal.aborted) setLoading(false)
      }
    }

    void run()
    return () => controller.abort()
  }, [currentPage, filterOData, pageSize, parseRows, table])

  useEffect(() => {
    if (!loading) disableClientFilteringRef.current = false
  }, [loading])

  const { rows, invalidCount, issues, error, totalCount } = result

  const hasPrev = currentPage > 1
  const hasNext =
    rows.length === pageSize &&
    (typeof totalCount === "number" ? currentPage * pageSize < totalCount : true)

  const lastPage =
    typeof totalCount === "number" ? Math.max(1, Math.ceil(totalCount / pageSize)) : undefined
  const canGoFirst = hasPrev
  const canGoLast = typeof lastPage === "number" ? currentPage < lastPage : false
  const hasFilters = !!filterOData || Object.keys(filterModel).length > 0

  function handleFitColumnsToHeader() {
    const api = gridApiRef.current as any
    if (!api) return

    const cols = (api?.getAllGridColumns?.() ?? api?.getAllColumns?.() ?? []) as any[]
    const widths = cols
      .map((c) => {
        const colId = c.getColId?.()
        const def = c.getColDef?.() ?? {}
        const label = String(def.headerName ?? def.field ?? colId ?? "")
        if (!colId) return null
        const approx = Math.ceil(label.length * 9 + 80)
        const newWidth = Math.max(140, Math.min(900, approx))
        return { key: colId, newWidth }
      })
      .filter(Boolean)

    api?.setColumnWidths?.(widths, true)
  }

  function handleFitColumnsToData() {
    const api = gridApiRef.current as any
    if (!api) return
    const cols = (api?.getAllGridColumns?.() ?? api?.getAllColumns?.() ?? []) as any[]
    const colIds = cols.map((c) => c.getColId?.()).filter(Boolean)
    api?.autoSizeColumns?.(colIds, true)
  }

  function handleResetFilters() {
    const api = gridApiRef.current
    if (!api) return
    api.setFilterModel(null)
    api.onFilterChanged?.()
    setFilterModel({})
    setFilterOData(undefined)
    setFilterLabel(undefined)
    setCurrentPage(1)
  }

  function handleClearSelection() {
    gridApiRef.current?.deselectAll()
    setSelectedCount(0)
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-col rounded-lg border border-[color:var(--color-border)] [background:var(--color-bg)] p-6 text-[color:var(--color-text)]">
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <h1 className="min-w-0 text-xl font-medium text-[color:var(--color-accent)]">{title}</h1>
            <a
              href={docUrl}
              target="_blank"
              rel="noopener noreferrer"
              aria-label="Open dataset docs in new window"
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent p-1.5 text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Open in new window"
            >
              <ExternalLinkIcon />
            </a>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <button
              type="button"
              onClick={handleFitColumnsToHeader}
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent px-2 py-1 text-sm text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Fit columns to header"
            >
              Fit Columns
            </button>
            <button
              type="button"
              onClick={handleFitColumnsToData}
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent px-2 py-1 text-sm text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Auto-size columns to content"
            >
              Fit Data
            </button>
            <HbcHelpTooltip items={helpItems} />
          </div>
        </div>

        {issues.length ? (
          <div className="rounded-md border border-yellow-500/30 bg-yellow-500/10 px-3 py-2 text-sm text-yellow-100">
            <div className="font-medium">Validation issues (sample)</div>
            <ul className="list-disc pl-5">
              {issues.map((x) => (
                <li key={x}>{x}</li>
              ))}
            </ul>
          </div>
        ) : null}
      </div>

      <div className="mt-4 flex min-h-0 flex-1 flex-col">
        <div className="min-h-0 flex-1">
          <HbcAgTable<T>
            rowData={rows}
            columnDefs={columnDefs}
            error={error ?? null}
            rowIdField={rowIdField}
            height="100%"
            loading={loading}
            onGridReady={(e: GridReadyEvent<T>) => {
              gridApiRef.current = e.api
            }}
            onSelectionChanged={(e: SelectionChangedEvent<T>) => {
              setSelectedCount(e.api.getSelectedNodes().length)
            }}
            onFilterPaste={() => {
              disableClientFilteringRef.current = true
            }}
            onCellDoubleClicked={(e: CellDoubleClickedEvent<T>) => {
              if (!e.node) return
              e.node.setSelected(!e.node.isSelected())
              setSelectedCount(e.api.getSelectedNodes().length)
            }}
            onFilterChanged={(e: FilterChangedEvent<T>) => {
              const next = e.api.getFilterModel()
              setFilterModel(next)

              if (filterDebounceRef.current) window.clearTimeout(filterDebounceRef.current)
              filterDebounceRef.current = window.setTimeout(() => {
                const odata = filterModelToOData(
                  next,
                  dateFieldSet.size ? dateFieldSet : undefined,
                  dateTimeFieldSet.size ? dateTimeFieldSet : undefined
                )
                setFilterOData(odata)
                setFilterLabel(odata)
                setCurrentPage(1)
              }, BACKEND_FILTER_DEBOUNCE_MS)
            }}
            gridOptions={{
              rowSelection: {
                mode: "multiRow",
                enableSelectionWithoutKeys: true,
                enableClickSelection: false,
                checkboxes: false,
                headerCheckbox: false,
              },
            }}
          />
        </div>

        <div className="mt-3 flex items-center justify-between gap-3">
          <div className="text-sm text-[color:var(--color-muted)]">
            {(() => {
              const total = typeof totalCount === "number" ? totalCount : undefined
              const last = total ? Math.max(1, Math.ceil(total / pageSize)) : undefined
              const start = (currentPage - 1) * pageSize + 1
              const end = (currentPage - 1) * pageSize + rows.length

              return (
                <>
                  Page {currentPage}
                  {last ? ` of ${last}` : ""}
                  {"; "}
                  Rows: {rows.length ? `${start}-${end}` : "0"}
                  {total ? `; Total Rows ${total}` : ""}
                  {invalidCount ? `; filtered ${invalidCount}` : ""}
                  {filterLabel ? `; filter ${filterLabel}` : ""}
                </>
              )
            })()}
          </div>

          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              aria-disabled={!canGoFirst}
              disabled={!canGoFirst}
              onClick={() => setCurrentPage(1)}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                canGoFirst ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="First page"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M18 18l-6-6 6-6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M12 18l-6-6 6-6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={!hasPrev}
              disabled={!hasPrev}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                hasPrev ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="Previous page"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M15 18l-6-6 6-6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={!hasNext}
              disabled={!hasNext}
              onClick={() => setCurrentPage((p) => p + 1)}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                hasNext ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="Next page"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M9 6l6 6-6 6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={!canGoLast}
              disabled={!canGoLast}
              onClick={() => typeof lastPage === "number" && setCurrentPage(lastPage)}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                canGoLast ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="Last page"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M6 6l6 6-6 6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M12 6l6 6-6 6"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={!hasFilters}
              disabled={!hasFilters}
              onClick={handleResetFilters}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                hasFilters ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="Reset filters"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M4 6h16"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M7 12h10"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M10 18h4"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={selectedCount === 0}
              disabled={selectedCount === 0}
              onClick={handleClearSelection}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                selectedCount ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title={selectedCount ? `Clear selection (${selectedCount})` : "Clear selection"}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                <path d="M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
