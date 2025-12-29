"use client"

import type { ColDef } from "ag-grid-community"
import type { MutableRefObject } from "react"

import { HbcMonikerGridPage } from "@/components/hbc/HbcMonikerGridPage"
import {
  dateOnlyFromText,
  formatDateOnly,
  formatDateTime,
  normalizeIsoishDateTime,
} from "@/components/hbc/dateTime"
import {
  NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMNS,
  parseNycOpenData311CallCenterInquiry,
  type NycOpenData311CallCenterInquiry,
} from "@/lib/models/nyc_open_data_311_call_center_inquiry"

const TABLE = "nyc_open_data_311_call_center_inquiry"
const CLIENT_CONTAINS_MIN_CHARS = 4

function buildColumnDefs(
  disableClientFilteringRef: MutableRefObject<boolean>
): ColDef<NycOpenData311CallCenterInquiry>[] {
  return NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMNS.map((field) => ({
    field: field as Extract<keyof NycOpenData311CallCenterInquiry, string>,
    filter: "agTextColumnFilter",
    filterParams:
      field === "date"
        ? {
            filterOptions: ["equals"],
            defaultOption: "equals",
            debounceMs: 300,
            textMatcher: ({
              value,
              filterText,
            }: {
              value: unknown
              filterText?: string | null
            }) => {
              if (disableClientFilteringRef.current) return true

              const ft = (filterText ?? "").toString().trim()
              if (!ft) return true

              const filterDate = dateOnlyFromText(ft)
              if (!filterDate) return true

              const cellDate = dateOnlyFromText((value ?? "").toString())
              return cellDate ? cellDate === filterDate : true
            },
          }
        : field === "date_time"
          ? {
              filterOptions: ["equals"],
              defaultOption: "equals",
              debounceMs: 300,
              textMatcher: ({
                value,
                filterText,
              }: {
                value: unknown
                filterText?: string | null
              }) => {
                if (disableClientFilteringRef.current) return true

                const ft = (filterText ?? "").toString().trim()
                if (!ft) return true

                const filterDateTime = normalizeIsoishDateTime(ft)
                if (filterDateTime) {
                  const cellDt = normalizeIsoishDateTime((value ?? "").toString())
                  return cellDt ? cellDt === filterDateTime : true
                }

                const filterDate = dateOnlyFromText(ft)
                if (!filterDate) return true

                const cellDate = dateOnlyFromText((value ?? "").toString())
                return cellDate ? cellDate === filterDate : true
              },
            }
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
    ...(field === "date" ? { valueFormatter: ({ value }) => formatDateOnly(value) } : {}),
    ...(field === "date_time" ? { valueFormatter: ({ value }) => formatDateTime(value) } : {}),
  }))
}

export default function NycOpenData311CallCenterInquiry({ docUrl }: { docUrl: string }) {
  return (
    <HbcMonikerGridPage<NycOpenData311CallCenterInquiry>
      title="NYC Open Data 311 Call Center Inquiry:"
      table={TABLE}
      docUrl={docUrl}
      dateFields={["date"]}
      dateTimeFields={["date_time"]}
      rowIdField="hbc_unique_key"
      buildColumnDefs={buildColumnDefs}
      parseRows={parseNycOpenData311CallCenterInquiry}
    />
  )
}
