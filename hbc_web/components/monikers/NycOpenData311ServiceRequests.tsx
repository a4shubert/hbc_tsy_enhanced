"use client"

import type { ColDef } from "ag-grid-community"
import type { MutableRefObject } from "react"

import { HbcMonikerGridPage } from "@/components/hbc/HbcMonikerGridPage"
import {
  dateOnlyFromText,
  formatDateTime,
  normalizeIsoishDateTime,
} from "@/components/hbc/dateTime"
import {
  NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS,
  parseNycOpenData311ServiceRequests,
  type NycOpenData311ServiceRequest,
} from "@/lib/models/nyc_open_data_311_service_requests"

const TABLE = "nyc_open_data_311_service_requests"
const CLIENT_CONTAINS_MIN_CHARS = 4

function buildColumnDefs(
  disableClientFilteringRef: MutableRefObject<boolean>
): ColDef<NycOpenData311ServiceRequest>[] {
  return NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS.map((field) => ({
    field: field as Extract<keyof NycOpenData311ServiceRequest, string>,
    filter: field.includes("date")
      ? "agTextColumnFilter"
      : field === "latitude" ||
          field === "longitude" ||
          field.endsWith("_coordinate_state_plane_")
        ? "agNumberColumnFilter"
        : "agTextColumnFilter",
    filterParams: field.includes("date")
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
      : field === "latitude" ||
          field === "longitude" ||
          field.endsWith("_coordinate_state_plane_")
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
    ...(field.includes("date") ? { valueFormatter: ({ value }) => formatDateTime(value) } : {}),
  }))
}

export default function NycOpenData311ServiceRequests({ docUrl }: { docUrl: string }) {
  return (
    <HbcMonikerGridPage<NycOpenData311ServiceRequest>
      title="NYC Open Data 311 Service Requests:"
      table={TABLE}
      docUrl={docUrl}
      dateTimeFields={NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS.filter((x) => x.includes("date"))}
      rowIdField="hbc_unique_key"
      buildColumnDefs={buildColumnDefs}
      parseRows={parseNycOpenData311ServiceRequests}
    />
  )
}
