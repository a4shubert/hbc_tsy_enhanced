/* eslint-disable no-console */
"use client"

import type { ColDef } from "ag-grid-community"
import { useEffect, useMemo, useState } from "react"

import { HbcAgTable } from "@/components/HbcAgTable"
import {
    NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS,
    parseNycOpenData311ServiceRequests,
    type NycOpenData311ServiceRequest,
} from "@/lib/models/nyc_open_data_311_service_requests"

const TABLE = "nyc_open_data_311_service_requests"
const PAGE_SIZE = 50

type PageResult = {
    rows: NycOpenData311ServiceRequest[]
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

export default function NycOpenData311ServiceRequests() {
    const [currentPage, setCurrentPage] = useState(1)

    const columnDefs = useMemo<ColDef<NycOpenData311ServiceRequest>[]>(
        () =>
            NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS.map((field) => ({
                field: field as Extract<keyof NycOpenData311ServiceRequest, string>,
                filter: field.includes("date") ? "agDateColumnFilter" : true,
            })),
        []
    )

    const [loading, setLoading] = useState(false)
    const [result, setResult] = useState<PageResult>({
        rows: [],
        invalidCount: 0,
        issues: [],
    })

    useEffect(() => {
        const controller = new AbortController()

        async function run() {
            const apiBase = "/backend"
            const skip = (currentPage - 1) * PAGE_SIZE
            const url = `${apiBase}/${TABLE}?$top=${PAGE_SIZE}&$skip=${skip}&$count=true`

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
                const parsed = parseNycOpenData311ServiceRequests(payload)

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
    }, [currentPage])

    const { rows, invalidCount, issues, error, totalCount } = result

    const hasPrev = currentPage > 1
    const hasNext =
        rows.length === PAGE_SIZE &&
        (typeof totalCount === "number" ? currentPage * PAGE_SIZE < totalCount : true)

    const lastPage =
        typeof totalCount === "number" ? Math.max(1, Math.ceil(totalCount / PAGE_SIZE)) : undefined
    const canGoFirst = hasPrev
    const canGoLast = typeof lastPage === "number" ? currentPage < lastPage : false

    return (
        <div className="w-full rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-bg)] p-6 text-[color:var(--color-text)]">
            <div className="flex flex-col gap-2">
                <h1 className="text-xl font-medium text-[color:var(--color-accent)]">
                    NYC Open Data 311 Service Requests:
                </h1>
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
                    loading={loading}
                    gridOptions={{
                        rowSelection: {
                            mode: "multiRow",
                            enableSelectionWithoutKeys: true,
                            enableClickSelection: true,
                            checkboxes: false,
                            headerCheckbox: false,
                        },
                    }}
                />

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
                    </div>
                </div>
            </div>
        </div>
    )
}
