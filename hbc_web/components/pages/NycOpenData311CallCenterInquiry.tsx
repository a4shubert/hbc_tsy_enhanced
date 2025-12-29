/* eslint-disable no-console */
"use client"

import type { CellDoubleClickedEvent } from "ag-grid-community"
import type { ColDef } from "ag-grid-community"
import type { FilterChangedEvent, FilterModel } from "ag-grid-community"
import type { GridApi, GridReadyEvent } from "ag-grid-community"
import type { SelectionChangedEvent } from "ag-grid-community"
import { useEffect, useMemo, useRef, useState } from "react"

import { HbcAgTable } from "@/components/HbcAgTable"
import { HbcHelpTooltip } from "@/components/HbcHelpTooltip"
import {
  NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMNS,
  parseNycOpenData311CallCenterInquiry,
  type NycOpenData311CallCenterInquiry,
} from "@/lib/models/nyc_open_data_311_call_center_inquiry"

const TABLE = "nyc_open_data_311_call_center_inquiry"
const PAGE_SIZE = 50
const CLIENT_CONTAINS_MIN_CHARS = 4
const SERVER_CONTAINS_MIN_CHARS = 3
const BACKEND_FILTER_DEBOUNCE_MS = 900

type PageResult = {
  rows: NycOpenData311CallCenterInquiry[]
  invalidCount: number
  issues: string[]
  totalCount?: number
  error?: string
}

function ChevronLeftIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M15 18l-6-6 6-6"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function ChevronRightIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M9 6l6 6-6 6"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function ChevronsLeftIcon() {
  return (
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
  )
}

function ChevronsRightIcon() {
  return (
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
  )
}

export default function NycOpenData311CallCenterInquiry({ docUrl }: { docUrl: string }) {
  const [currentPage, setCurrentPage] = useState(1)
  const [filterModel, setFilterModel] = useState<FilterModel>({})
  const [filterOData, setFilterOData] = useState<string | undefined>(undefined)
  const [filterLabel, setFilterLabel] = useState<string | undefined>(undefined)
  const filterDebounceRef = useRef<number | null>(null)
  const disableClientFilteringRef = useRef(false)
  const gridApiRef = useRef<GridApi<NycOpenData311CallCenterInquiry> | null>(null)
  const [selectedCount, setSelectedCount] = useState(0)

  const columnDefs = useMemo<ColDef<NycOpenData311CallCenterInquiry>[]>(
    () =>
      NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMNS.map((field) => ({
        field: field as Extract<keyof NycOpenData311CallCenterInquiry, string>,
        filter: field === "date" || field === "date_time" ? "agDateColumnFilter" : "agTextColumnFilter",
        filterParams:
          field === "date" || field === "date_time"
            ? { filterOptions: ["equals"], defaultOption: "equals", debounceMs: 300 }
            : {
              filterOptions: ["contains", "equals"],
              defaultOption: "contains",
              debounceMs: 150,
              textMatcher: ({
                value,
                filterText,
                filterOption,
              }: {
                value: unknown
                filterText?: string | null
                filterOption?: string
              }) => {
                if (disableClientFilteringRef.current) return true
                const cell = (value ?? "").toString().toLowerCase()
                const ft = (filterText ?? "").toString().trim().toLowerCase()
                if (!ft) return true

                if (filterOption === "contains") {
                  if (ft.length < CLIENT_CONTAINS_MIN_CHARS) return true
                  return cell.includes(ft)
                }

                if (filterOption === "equals") return cell === ft
                return true
              },
            },
      })),
    []
  )

  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<PageResult>({
    rows: [],
    invalidCount: 0,
    issues: [],
  })

  function escapeOdataString(value: string) {
    return value.replace(/'/g, "''")
  }

  function filterModelToOData(model: FilterModel | null | undefined) {
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

        if (type === "contains") parts.push(`contains(${f},'${v}')`)
        else parts.push(`${f} eq '${v}'`)
        continue
      }

      if (typeof m.filter === "number" && Number.isFinite(m.filter)) {
        parts.push(`${f} eq ${m.filter}`)
        continue
      }

      if (typeof m.dateFrom === "string" && m.dateFrom.trim()) {
        const v = `${m.dateFrom.trim()}T00:00:00`
        parts.push(`${f} eq '${v}'`)
        continue
      }
    }

    return parts.length ? parts.join(" and ") : undefined
  }

  useEffect(() => {
    const controller = new AbortController()

    async function run() {
      const apiBase = "/backend"
      const skip = (currentPage - 1) * PAGE_SIZE
      const params = new URLSearchParams()
      params.set("$top", String(PAGE_SIZE))
      params.set("$skip", String(skip))
      params.set("$count", "true")
      if (filterOData) params.set("$filter", filterOData)
      const url = `${apiBase}/${TABLE}?${params.toString()}`

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
        console.log(`[hbc_web] GET ${url} ->`, json)

        const hasValue = typeof json === "object" && json !== null && "value" in (json as any)
        const payload = hasValue ? (json as any).value : json
        const parsed = parseNycOpenData311CallCenterInquiry(payload)

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
  }, [currentPage, filterOData])

  useEffect(() => {
    if (!loading) disableClientFilteringRef.current = false
  }, [loading])

  const { rows, invalidCount, issues, error, totalCount } = result

  const hasPrev = currentPage > 1
  const hasNext =
    rows.length === PAGE_SIZE &&
    (typeof totalCount === "number" ? currentPage * PAGE_SIZE < totalCount : true)

  const lastPage =
    typeof totalCount === "number" ? Math.max(1, Math.ceil(totalCount / PAGE_SIZE)) : undefined
  const canGoFirst = hasPrev
  const canGoLast = typeof lastPage === "number" ? currentPage < lastPage : false
  const hasFilters = !!filterOData || Object.keys(filterModel).length > 0

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-col rounded-lg border border-[color:var(--color-border)] [background:var(--color-bg)] p-6 text-[color:var(--color-text)]">
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <h1 className="min-w-0 text-xl font-medium text-[color:var(--color-accent)]">
              NYC Open Data 311 Call Center Inquiry:
            </h1>
            <a
              href={docUrl}
              target="_blank"
              rel="noopener noreferrer"
              aria-label="Open dataset docs in new window"
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent p-1.5 text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Dataset description"
            >
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
            </a>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <button
              type="button"
              onClick={() => {
                const api = gridApiRef.current as any
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
              }}
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent px-2 py-1 text-sm text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Fit columns to header"
            >
              Fit Columns
            </button>
            <button
              type="button"
              onClick={() => {
                const api = gridApiRef.current as any
                const cols = (api?.getAllGridColumns?.() ?? api?.getAllColumns?.() ?? []) as any[]
                const colIds = cols.map((c) => c.getColId?.()).filter(Boolean)
                api?.autoSizeColumns?.(colIds, true)
              }}
              className="inline-flex shrink-0 items-center justify-center rounded-md border border-transparent px-2 py-1 text-sm text-[color:var(--color-muted)] hover:border-[color:var(--color-border)] hover:text-[color:var(--color-accent)]"
              title="Auto-size columns to content"
            >
              Fit Data
            </button>
            <HbcHelpTooltip
              items={[
                "Single click focuses a cell; copy with Cmd+C (macOS) or Ctrl+C (Windows/Linux).",
                "Double click a cell to toggle selecting its entire row.",
                "After typing in a filter, press Enter to switch from contains → equals.",
                "Use the X button to clear any selected rows (de-select all).",
                "Use the filter-reset button to clear all column filters.",
                "Pagination buttons move between pages (first/prev/next/last); filters affect server-side results and paging.",
              ]}
            />
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
          <HbcAgTable<NycOpenData311CallCenterInquiry>
            rowData={rows}
            columnDefs={columnDefs}
            error={error ?? null}
            rowIdField="hbc_unique_key"
            height="100%"
            loading={loading}
            onGridReady={(e: GridReadyEvent<NycOpenData311CallCenterInquiry>) => {
              gridApiRef.current = e.api
            }}
            onSelectionChanged={(e: SelectionChangedEvent<NycOpenData311CallCenterInquiry>) => {
              setSelectedCount(e.api.getSelectedNodes().length)
            }}
            onFilterPaste={() => {
              disableClientFilteringRef.current = true
            }}
            onCellDoubleClicked={(e: CellDoubleClickedEvent<NycOpenData311CallCenterInquiry>) => {
              if (!e.node) return
              e.node.setSelected(!e.node.isSelected())
              setSelectedCount(e.api.getSelectedNodes().length)
            }}
            onFilterChanged={(e: FilterChangedEvent<NycOpenData311CallCenterInquiry>) => {
              const next = e.api.getFilterModel()
              setFilterModel(next)

              if (filterDebounceRef.current) window.clearTimeout(filterDebounceRef.current)
              filterDebounceRef.current = window.setTimeout(() => {
                const odata = filterModelToOData(next)
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
              const lastPage = total ? Math.max(1, Math.ceil(total / PAGE_SIZE)) : undefined
              const start = (currentPage - 1) * PAGE_SIZE + 1
              const end = (currentPage - 1) * PAGE_SIZE + rows.length

              return (
                <>
                  Page {currentPage}
                  {lastPage ? ` of ${lastPage}` : ""}
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
              <ChevronsLeftIcon />
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
              <ChevronLeftIcon />
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
              <ChevronRightIcon />
            </button>

            <button
              type="button"
              aria-disabled={!canGoLast}
              disabled={!canGoLast}
              onClick={() => {
                if (typeof lastPage === "number") setCurrentPage(lastPage)
              }}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                canGoLast ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title={typeof lastPage === "number" ? `Last page (${lastPage})` : "Last page"}
            >
              <ChevronsRightIcon />
            </button>

            <button
              type="button"
              aria-disabled={!hasFilters}
              disabled={!hasFilters}
              onClick={() => {
                if (filterDebounceRef.current) {
                  window.clearTimeout(filterDebounceRef.current)
                  filterDebounceRef.current = null
                }

                gridApiRef.current?.setFilterModel(null)
                setFilterModel({})
                setFilterOData(undefined)
                setFilterLabel(undefined)
                setCurrentPage(1)
              }}
              className={[
                "inline-flex items-center justify-center rounded-md border px-2.5 py-2 text-sm",
                "border-[color:var(--color-border)] bg-[color:var(--color-bg)] text-[color:var(--color-text)]",
                hasFilters ? "hover:border-[color:var(--color-accent)]" : "opacity-50",
              ].join(" ")}
              title="Reset filters"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M3 6h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                <path d="M6 12h12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                <path d="M10 18h4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </button>

            <button
              type="button"
              aria-disabled={selectedCount === 0}
              disabled={selectedCount === 0}
              onClick={() => {
                gridApiRef.current?.deselectAll()
                setSelectedCount(0)
              }}
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
