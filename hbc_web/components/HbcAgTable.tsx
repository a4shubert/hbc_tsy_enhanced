// HbcAgTable.tsx
"use client"

import "ag-grid-community/styles/ag-grid.css"
import "ag-grid-community/styles/ag-theme-quartz.css"
import "./HbcAgTable.theme.css"

import { AllCommunityModule, ModuleRegistry } from "ag-grid-community"
import type { ColDef, GridApi, GridOptions } from "ag-grid-community"
import type { CellClickedEvent, CellDoubleClickedEvent } from "ag-grid-community"
import type { CellKeyDownEvent } from "ag-grid-community"
import type { FilterChangedEvent, GridReadyEvent, SortChangedEvent } from "ag-grid-community"
import type { SelectionChangedEvent } from "ag-grid-community"
import { AgGridReact } from "ag-grid-react"
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
}

export function HbcAgTable<T extends Record<string, unknown>>({
  rowData,
  columnDefs,
  className,
  height = "70vh",
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
}: HbcAgTableProps<T>) {
  const gridApiRef = useRef<GridApi<T> | null>(null)

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

  function handleKeyDownCapture(ev: React.KeyboardEvent) {
    const key = ev.key?.toLowerCase()
    const isCopy = (ev.metaKey || ev.ctrlKey) && key === "c" && !ev.shiftKey && !ev.altKey
    if (!isCopy) return

    const text = getFocusedCellText()
    if (!text) return

    ev.preventDefault()
    void copyText(text)
  }
  const autoColumnDefs = useMemo<ColDef<T>[]>(() => {
    if (columnDefs && columnDefs.length) return columnDefs
    const keys = Object.keys(rowData?.[0] ?? {})
    return keys.map((field) => ({ field })) as ColDef<T>[]
  }, [columnDefs, rowData])

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      resizable: true,
      sortable: true,
      unSortIcon: true,
      filter: true,
      floatingFilter: true,
      minWidth: 140,
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

      // key bits
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

  return (
    <div className="w-full" onKeyDownCapture={handleKeyDownCapture}>
      {error ? (
        <div className="mb-3 rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">
          {error}
        </div>
      ) : null}

      <div className={finalClassName} style={{ height }}>
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
