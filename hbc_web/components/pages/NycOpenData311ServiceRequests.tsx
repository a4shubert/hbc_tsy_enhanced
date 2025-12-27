import type { ColDef } from "ag-grid-community"

import { HbcAgTable } from "@/components/HbcAgTable"
import {
  parseNycOpenData311ServiceRequests,
  type NycOpenData311ServiceRequest,
} from "@/lib/models/nyc_open_data_311_service_requests"

const TABLE = "nyc_open_data_311_service_requests"

function buildColumnDefs(): ColDef<NycOpenData311ServiceRequest>[] {
  const cols = [
    "hbc_unique_key",
    "unique_key",
    "created_date",
    "closed_date",
    "status",
    "agency",
    "agency_name",
    "complaint_type",
    "descriptor",
    "borough",
    "incident_zip",
    "incident_address",
    "city",
    "latitude",
    "longitude",
  ] as const satisfies ReadonlyArray<string>

  return cols.map((field) => ({
    field: field as Extract<keyof NycOpenData311ServiceRequest, string>,
    filter: field.includes("date") ? "agDateColumnFilter" : true,
  }))
}

async function getFirst50(): Promise<{
  rows: NycOpenData311ServiceRequest[]
  invalidCount: number
  issues: string[]
  error?: string
}> {
  const apiBase = (process.env.HBC_API_URL || "http://localhost:5047").replace(/\/$/, "")
  const url = `${apiBase}/${TABLE}?$top=50&$skip=0`

  try {
    const res = await fetch(url, { cache: "no-store" })
    if (!res.ok) {
      return {
        rows: [],
        invalidCount: 0,
        issues: [],
        error: `REST API error ${res.status} on GET ${url}`,
      }
    }

    const json = await res.json()
    const payload = typeof json === "object" && json && "value" in json ? (json as any).value : json
    return parseNycOpenData311ServiceRequests(payload)
  } catch (e) {
    return {
      rows: [],
      invalidCount: 0,
      issues: [],
      error: `Failed to fetch ${url}: ${e instanceof Error ? e.message : String(e)}`,
    }
  }
}

export default async function NycOpenData311ServiceRequests() {
  const { rows, invalidCount, issues, error } = await getFirst50()
  const columnDefs = buildColumnDefs()

  return (
    <div className="w-full rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-card)] p-6 text-[color:var(--color-text)]">
      <div className="flex flex-col gap-2">
        <h1 className="text-xl font-medium text-[color:var(--color-accent)]">
          NYC Open Data 311 — Service Requests (first 50)
        </h1>
        <div className="text-sm text-[color:var(--color-muted)]">
          Endpoint: <span className="font-mono">{`/${TABLE}?$top=50&$skip=0`}</span>
          {invalidCount ? ` · filtered ${invalidCount} invalid row(s)` : null}
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

      <div className="mt-4">
        <HbcAgTable<NycOpenData311ServiceRequest>
          rowData={rows}
          columnDefs={columnDefs}
          error={error ?? null}
          rowIdField="hbc_unique_key"
          height={"50vh"}
        />
      </div>
    </div>
  )
}
