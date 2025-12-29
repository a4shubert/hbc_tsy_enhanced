// components/DatasetDashboard.tsx
"use client"

import { useMemo, useState } from "react"

import NycOpenData311CallCenterInquiry from "@/components/pages/NycOpenData311CallCenterInquiry"
import NycOpenData311CustomerSatisfactionSurvey from "@/components/pages/NycOpenData311CustomerSatisfactionSurvey"
import NycOpenData311ServiceRequests from "@/components/pages/NycOpenData311ServiceRequests"

type DatasetKey = "service_requests" | "customer_satisfaction_survey" | "call_center_inquiry"

type DatasetDef = {
  key: DatasetKey
  label: string
  moniker: string
  render: () => React.ReactNode
}

function MenuIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M4 6h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M4 12h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M4 18h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

function CloseIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      <path d="M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

function SidebarContent({
  datasets,
  selected,
  onSelect,
}: {
  datasets: DatasetDef[]
  selected: DatasetKey
  onSelect: (key: DatasetKey) => void
}) {
  return (
    <div className="flex h-full flex-col gap-4 p-4 [background:var(--color-bg)]">
      <div className="text-lg font-medium text-[color:var(--color-accent)]">DataSets</div>
      <div className="flex flex-col gap-3">
        {datasets.map((d) => (
          <label
            key={d.key}
            className="flex cursor-pointer items-start gap-2 rounded-md border border-[color:var(--color-border)] [background:var(--color-card)] px-3 py-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
          >
            <input
              type="radio"
              name="dataset"
              value={d.key}
              checked={selected === d.key}
              onChange={() => onSelect(d.key)}
              className="mt-1 accent-[color:var(--color-accent)]"
            />
            <span className="min-w-0">
              <div className="text-sm font-medium">{d.label}</div>
              <div className="text-xs text-[color:var(--color-muted)]">{d.moniker}</div>
            </span>
          </label>
        ))}
      </div>
    </div>
  )
}

export function DatasetDashboard() {
  const datasets = useMemo<DatasetDef[]>(
    () => [
      {
        key: "service_requests",
        label: "NYC 311 Service Requests",
        moniker: "nyc_open_data_311_service_requests",
        render: () => <NycOpenData311ServiceRequests />,
      },
      {
        key: "customer_satisfaction_survey",
        label: "NYC 311 Customer Satisfaction Survey",
        moniker: "nyc_open_data_311_customer_satisfaction_survey",
        render: () => <NycOpenData311CustomerSatisfactionSurvey />,
      },
      {
        key: "call_center_inquiry",
        label: "NYC 311 Call Center Inquiry",
        moniker: "nyc_open_data_311_call_center_inquiry",
        render: () => <NycOpenData311CallCenterInquiry />,
      },
    ],
    []
  )

  const [selected, setSelected] = useState<DatasetKey>(datasets[0]?.key ?? "service_requests")
  const [drawerOpen, setDrawerOpen] = useState(false)

  const active = datasets.find((d) => d.key === selected) ?? datasets[0]

  return (
    <div className="flex h-full min-h-0 w-full gap-6">
      {/* Static sidebar for >=1920px */}
      <aside className="hidden h-full w-[15vw] shrink-0 rounded-lg border border-[color:var(--color-border)] [background:var(--color-card)] [@media(min-width:1920px)]:block">
        <SidebarContent
          datasets={datasets}
          selected={selected}
          onSelect={(k) => {
            setSelected(k)
          }}
        />
      </aside>

      {/* Hamburger / drawer for <1920px */}
      <div className="block [@media(min-width:1920px)]:hidden">
        <button
          type="button"
          aria-label="Open dataset menu"
          onClick={() => setDrawerOpen(true)}
          className="inline-flex items-center justify-center rounded-md border border-[color:var(--color-border)] [background:var(--color-bg)] p-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
        >
          <MenuIcon />
        </button>

        {drawerOpen ? (
          <div className="fixed inset-0 z-[60]">
            <button
              type="button"
              aria-label="Close dataset menu"
              onClick={() => setDrawerOpen(false)}
              className="absolute inset-0 cursor-default bg-black/50"
            />
            <div className="absolute left-0 top-0 h-full w-[15vw] min-w-[260px] border-r border-[color:var(--color-border)] [background:var(--color-card)]">
              <div className="flex items-center justify-between border-b border-[color:var(--color-border)] p-3">
                <div className="text-sm font-medium text-[color:var(--color-accent)]">DataSets</div>
                <button
                  type="button"
                  aria-label="Close"
                  onClick={() => setDrawerOpen(false)}
                  className="inline-flex items-center justify-center rounded-md border border-[color:var(--color-border)] [background:var(--color-bg)] p-2 text-[color:var(--color-text)] hover:border-[color:var(--color-accent)]"
                >
                  <CloseIcon />
                </button>
              </div>
              <SidebarContent
                datasets={datasets}
                selected={selected}
                onSelect={(k) => {
                  setSelected(k)
                  setDrawerOpen(false)
                }}
              />
            </div>
          </div>
        ) : null}
      </div>

      <div className="min-h-0 min-w-0 flex-1">
        {active?.render()}
      </div>
    </div>
  )
}
