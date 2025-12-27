"use client";

import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-quartz.css";

import type { ColDef, GridOptions } from "ag-grid-community";
import { AgGridReact } from "ag-grid-react";
import { useMemo } from "react";

export type HbcAgTableProps<T extends Record<string, unknown>> = {
  rowData: T[];
  columnDefs?: ColDef<T>[];
  className?: string;
  height?: number | string;
  loading?: boolean;
  error?: string | null;
  gridOptions?: GridOptions<T>;
  rowIdField?: Extract<keyof T, string>;
};

export function HbcAgTable<T extends Record<string, unknown>>({
  rowData,
  columnDefs,
  className,
  height = "70vh",
  loading,
  error,
  gridOptions,
  rowIdField,
}: HbcAgTableProps<T>) {
  const autoColumnDefs = useMemo<ColDef<T>[]>(() => {
    if (columnDefs && columnDefs.length) return columnDefs;
    const keys = Object.keys(rowData?.[0] ?? {});
    return keys.map((field) => ({ field })) as ColDef<T>[];
  }, [columnDefs, rowData]);

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      resizable: true,
      sortable: true,
      filter: true,
      floatingFilter: true,
      flex: 1,
      minWidth: 140,
    }),
    []
  );

  const mergedGridOptions = useMemo<GridOptions<T>>(
    () => ({
      animateRows: true,
      rowSelection: "single",
      suppressCellFocus: true,
      pagination: false,
      ...(gridOptions ?? {}),
    }),
    [gridOptions]
  );

  const finalClassName = [
    "ag-theme-quartz-dark w-full rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-card)]",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="w-full">
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
          getRowId={
            rowIdField
              ? (p) => {
                const raw = p.data?.[rowIdField];
                if (typeof raw === "string" && raw.length) return raw;
                if (typeof raw === "number") return String(raw);
                return JSON.stringify(p.data);
              }
              : undefined
          }
        />
      </div>
    </div>
  );
}
