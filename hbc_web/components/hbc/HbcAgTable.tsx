// HbcAgTable.tsx
"use client"

import "ag-grid-community/styles/ag-grid.css"
import "ag-grid-community/styles/ag-theme-quartz.css"

import { AllCommunityModule, ModuleRegistry } from "ag-grid-community"
import type { ColDef, GridApi, GridOptions } from "ag-grid-community"
import type { CellClickedEvent, CellDoubleClickedEvent } from "ag-grid-community"
import type { CellKeyDownEvent } from "ag-grid-community"
import type { FilterChangedEvent, GridReadyEvent, SortChangedEvent } from "ag-grid-community"
import type { SelectionChangedEvent } from "ag-grid-community"
import { AgGridReact } from "ag-grid-react"
import type { CSSProperties } from "react"
import type { KeyboardEvent as ReactKeyboardEvent } from "react"
import { useMemo, useRef } from "react"

let agGridRegistered = false
if (!agGridRegistered) {
  ModuleRegistry.registerModules([AllCommunityModule])
  agGridRegistered = true
}

export type HbcAgTableProps<T extends Record<string, unknown>> = {
  rowData: T[]
  columnDefs?: ColDef<T>[]
  className?: string
  height?: number | string
  loading?: boolean
  error?: string | null
  gridOptions?: GridOptions<T>
  rowIdField?: Extract<keyof T, string>
  onGridReady?: (event: GridReadyEvent<T>) => void
  onFilterChanged?: (event: FilterChangedEvent<T>) => void
  onSortChanged?: (event: SortChangedEvent<T>) => void
  onSelectionChanged?: (event: SelectionChangedEvent<T>) => void
  onCellClicked?: (event: CellClickedEvent<T>) => void
  onCellDoubleClicked?: (event: CellDoubleClickedEvent<T>) => void
  onCellKeyDown?: (event: CellKeyDownEvent<T>) => void
  onFilterPaste?: (colId: string) => void
}

export function HbcAgTable<T extends Record<string, unknown>>({
  rowData,
  columnDefs,
  className,
  height = "100%",
  loading,
  error,
  gridOptions,
  rowIdField,
  onGridReady,
  onFilterChanged,
  onSortChanged,
  onSelectionChanged,
  onCellClicked,
  onCellDoubleClicked,
  onCellKeyDown,
  onFilterPaste,
}: HbcAgTableProps<T>) {
  const gridApiRef = useRef<GridApi<T> | null>(null)
  const columnFilterTypeByColId = useMemo(() => {
    const map = new Map<string, unknown>()
    for (const def of columnDefs ?? []) {
      const colId = (def.colId ?? def.field) as string | undefined
      if (!colId) continue
      map.set(colId, def.filter)
    }
    return map
  }, [columnDefs])

  async function copyText(text: string) {
    try {
      if (navigator?.clipboard?.writeText) {
        await navigator.clipboard.writeText(text)
        return
      }
    } catch {
      // ignore and fall back
    }

    const el = document.createElement("textarea")
    el.value = text
    el.setAttribute("readonly", "true")
    el.style.position = "fixed"
    el.style.top = "-9999px"
    document.body.appendChild(el)
    el.select()
    document.execCommand("copy")
    document.body.removeChild(el)
  }

  function handleCellKeyDown(e: CellKeyDownEvent<T>) {
    onCellKeyDown?.(e)

    const ev = e.event as KeyboardEvent | undefined
    if (!ev) return

    const key = ev.key?.toLowerCase()
    const isCopy = (ev.metaKey || ev.ctrlKey) && key === "c" && !ev.shiftKey && !ev.altKey
    if (!isCopy) return

    const value = e.value
    if (value === null || value === undefined) return
    const text = typeof value === "string" ? value : String(value)

    ev.preventDefault()
    void copyText(text)
  }

  function findFloatingFilterColumnId(target: EventTarget | null): string | null {
    if (!target || !(target instanceof HTMLElement)) return null
    const direct = target.closest("[col-id]")?.getAttribute("col-id")
    if (direct && direct.length) return direct

    const floating = target.closest(".ag-floating-filter")
    if (!floating) return null

    const headerCell = target.closest(".ag-header-cell")
    const colId = headerCell?.getAttribute("col-id")
    return colId && colId.length ? colId : null
  }

  function setEqualsForTextFilter(colId: string, filterText?: string) {
    const api = gridApiRef.current
    if (!api) return

    const model = (api.getFilterModel() as Record<string, any>) ?? {}
    const existing = model[colId]
    const nextText = typeof filterText === "string" ? filterText : existing?.filter
    if (typeof nextText !== "string") return
    const trimmed = nextText.trim()
    if (!trimmed) return

    const existingType = existing?.type ? String(existing.type).toLowerCase() : ""
    const existingValue = typeof existing?.filter === "string" ? existing.filter.trim() : ""
    if (existingType === "equals" && existingValue === trimmed) return

    api.setFilterModel({
      ...model,
      [colId]: {
        ...(existing ?? {}),
        filterType: (existing?.filterType as string | undefined) ?? "text",
        type: "equals",
        filter: trimmed,
      },
    })
    api.onFilterChanged?.()
  }

  function parseDateFromText(input: string) {
    const text = input.trim()
    if (!text) return null

    const isoMatch = /^(\d{4}-\d{2}-\d{2})/.exec(text)
    if (isoMatch) return isoMatch[1]

    const ms = Date.parse(text)
    if (!Number.isFinite(ms)) return null

    const d = new Date(ms)
    const yyyy = String(d.getFullYear()).padStart(4, "0")
    const mm = String(d.getMonth() + 1).padStart(2, "0")
    const dd = String(d.getDate()).padStart(2, "0")
    return `${yyyy}-${mm}-${dd}`
  }

  function setEqualsForDateFilter(colId: string, filterText?: string) {
    const api = gridApiRef.current
    if (!api) return

    const model = (api.getFilterModel() as Record<string, any>) ?? {}
    const existing = model[colId]

    const nextText = typeof filterText === "string" ? filterText : existing?.dateFrom
    if (typeof nextText !== "string") return

    const dateFrom = parseDateFromText(nextText)
    if (!dateFrom) return

    const existingType = existing?.type ? String(existing.type).toLowerCase() : ""
    const existingDateFrom = typeof existing?.dateFrom === "string" ? existing.dateFrom.trim() : ""
    if (existingType === "equals" && existingDateFrom === dateFrom) return

    api.setFilterModel({
      ...model,
      [colId]: {
        ...(existing ?? {}),
        filterType: "date",
        type: "equals",
        dateFrom,
      },
    })
    api.onFilterChanged?.()
  }

  function getPreferredFilterType(colId: string) {
    const api = gridApiRef.current
    const model = (api?.getFilterModel() as Record<string, any>) ?? {}
    const existing = model[colId]
    const existingFilterType = typeof existing?.filterType === "string" ? existing.filterType : undefined
    if (existingFilterType) return existingFilterType

    const colFilter = columnFilterTypeByColId.get(colId)
    if (colFilter === "agDateColumnFilter") return "date"
    if (colFilter === "agNumberColumnFilter") return "number"
    return "text"
  }

  function handleKeyDownCapture(ev: ReactKeyboardEvent) {
    const key = ev.key?.toLowerCase()
    const isCopy = (ev.metaKey || ev.ctrlKey) && key === "c" && !ev.shiftKey && !ev.altKey
    const isPaste = (ev.metaKey || ev.ctrlKey) && key === "v" && !ev.shiftKey && !ev.altKey

    if (!isCopy && !isPaste) return

    const colId = findFloatingFilterColumnId(ev.target)
    if (!colId) {
      if (isCopy) {
        const text = getFocusedCellText()
        if (!text) return
        ev.preventDefault()
        void copyText(text)
      }
      return
    }

    if (isPaste) {
      onFilterPaste?.(colId)
      window.setTimeout(() => {
        const el = ev.target
        const value =
          el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement ? el.value : undefined
        const filterType = getPreferredFilterType(colId)
        if (filterType === "date") {
          setEqualsForDateFilter(colId, value)
          return
        }
        setEqualsForTextFilter(colId, value)
      }, 40)
      return
    }

    if (isCopy) {
      const text = getFocusedCellText()
      if (!text) return
      ev.preventDefault()
      void copyText(text)
    }
  }

  function handlePasteCapture(ev: React.ClipboardEvent) {
    const colId = findFloatingFilterColumnId(ev.target)
    if (!colId) return

    onFilterPaste?.(colId)
    window.setTimeout(() => {
      const el = ev.target
      const value = el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement ? el.value : undefined
      const filterType = getPreferredFilterType(colId)
      if (filterType === "date") {
        setEqualsForDateFilter(colId, value)
        return
      }
      setEqualsForTextFilter(colId, value)
    }, 40)
  }

  function getFocusedCellText(): string | null {
    const api = gridApiRef.current
    if (!api) return null

    const focused = api.getFocusedCell?.()
    if (!focused) return null

    const rowNode = api.getDisplayedRowAtIndex?.(focused.rowIndex)
    const colId = focused.column?.getColId?.()
    if (!rowNode || !colId) return null

    const value = (rowNode.data as any)?.[colId]
    if (value === null || value === undefined) return null
    return typeof value === "string" ? value : String(value)
  }

  const autoColumnDefs = useMemo<ColDef<T>[]>(() => {
    if (columnDefs && columnDefs.length) return columnDefs
    const keys = Object.keys(rowData?.[0] ?? {})
    return keys.map((field) => ({ field })) as ColDef<T>[]
  }, [columnDefs, rowData])

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      resizable: true,
      suppressMovable: false,
      sortable: true,
      unSortIcon: true,
      filter: true,
      floatingFilter: true,
      minWidth: 90,
    }),
    []
  )

  const mergedGridOptions = useMemo<GridOptions<T>>(
    () => ({
      theme: "legacy",
      animateRows: true,
      rowSelection: "single",
      suppressCellFocus: false,
      pagination: false,

      suppressMovableColumns: false,
      alwaysShowHorizontalScroll: true,
      suppressHorizontalScroll: false,
      suppressRowClickSelection: true,
      enableCellTextSelection: true,
      ensureDomOrder: true,

      ...(gridOptions ?? {}),
    }),
    [gridOptions]
  )

  const finalClassName = [
    "hbc-ag-grid ag-theme-quartz-dark w-full rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-card)]",
    className,
  ]
    .filter(Boolean)
    .join(" ")

  const gridContainerStyle = useMemo(() => {
    return {
      height: "100%",
      "--ag-border-color": "rgba(255, 255, 255, 0.16)",
      "--ag-secondary-border-color": "rgba(255, 255, 255, 0.12)",
      "--ag-header-column-resize-handle-color": "rgba(255, 255, 255, 0.65)",
      "--ag-header-column-resize-handle-width": "4px",
      "--ag-header-column-resize-handle-height": "60%",
    } as CSSProperties
  }, [])

  return (
    <div
      className="w-full"
      style={{ height }}
      onKeyDownCapture={handleKeyDownCapture}
      onPasteCapture={handlePasteCapture}
    >
      {error ? (
        <div className="mb-3 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">
          {error}
        </div>
      ) : null}

      <div className={finalClassName} style={gridContainerStyle}>
        <style jsx global>{`
          .hbc-ag-grid .ag-header-cell-resize {
            cursor: col-resize;
          }
          .hbc-ag-grid .ag-header-cell-resize::after {
            cursor: col-resize;
          }
        `}</style>
        <AgGridReact<T>
          rowData={rowData}
          columnDefs={autoColumnDefs}
          defaultColDef={defaultColDef}
          gridOptions={mergedGridOptions}
          loading={loading}
          onGridReady={(e) => {
            gridApiRef.current = e.api
            onGridReady?.(e)
          }}
          onFilterChanged={onFilterChanged}
          onSortChanged={onSortChanged}
          onSelectionChanged={onSelectionChanged}
          onCellClicked={onCellClicked}
          onCellDoubleClicked={onCellDoubleClicked}
          onCellKeyDown={handleCellKeyDown}
          getRowId={
            rowIdField
              ? (p) => {
                  const raw = p.data?.[rowIdField]
                  if (typeof raw === "string" && raw.length) return raw
                  if (typeof raw === "number") return String(raw)
                  return JSON.stringify(p.data)
                }
              : undefined
          }
        />
      </div>
    </div>
  )
}
