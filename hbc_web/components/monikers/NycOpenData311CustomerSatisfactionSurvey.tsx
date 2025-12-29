"use client"

import type { ColDef } from "ag-grid-community"
import type { MutableRefObject } from "react"

import { HbcMonikerGridPage } from "@/components/hbc/HbcMonikerGridPage"
import {
  NYC_OPEN_DATA_311_CUSTOMER_SATISFACTION_SURVEY_COLUMNS,
  parseNycOpenData311CustomerSatisfactionSurvey,
  type NycOpenData311CustomerSatisfactionSurvey,
} from "@/lib/models/nyc_open_data_311_customer_satisfaction_survey"

const TABLE = "nyc_open_data_311_customer_satisfaction_survey"
const CLIENT_CONTAINS_MIN_CHARS = 4

function buildColumnDefs(
  disableClientFilteringRef: MutableRefObject<boolean>
): ColDef<NycOpenData311CustomerSatisfactionSurvey>[] {
  return NYC_OPEN_DATA_311_CUSTOMER_SATISFACTION_SURVEY_COLUMNS.map((field) => ({
    field: field as Extract<keyof NycOpenData311CustomerSatisfactionSurvey, string>,
    filter:
      field === "nps"
        ? "agNumberColumnFilter"
        : field === "start_time" || field === "completion_time"
          ? "agDateColumnFilter"
          : "agTextColumnFilter",
    filterParams:
      field === "nps"
        ? { filterOptions: ["equals"], defaultOption: "equals", debounceMs: 300 }
        : field === "start_time" || field === "completion_time"
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
  }))
}

export default function NycOpenData311CustomerSatisfactionSurvey({ docUrl }: { docUrl: string }) {
  return (
    <HbcMonikerGridPage<NycOpenData311CustomerSatisfactionSurvey>
      title="NYC Open Data 311 Customer Satisfaction Survey:"
      table={TABLE}
      docUrl={docUrl}
      rowIdField="hbc_unique_key"
      buildColumnDefs={buildColumnDefs}
      parseRows={parseNycOpenData311CustomerSatisfactionSurvey}
    />
  )
}
