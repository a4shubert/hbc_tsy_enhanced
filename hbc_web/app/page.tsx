"use client"

import { useMemo, useState } from "react"

import { DatasetSidebar } from "@/components/DatasetSidebar"
import NycOpenData311CallCenterInquiry from "@/components/pages/NycOpenData311CallCenterInquiry"
import NycOpenData311CustomerSatisfactionSurvey from "@/components/pages/NycOpenData311CustomerSatisfactionSurvey"
import NycOpenData311ServiceRequests from "@/components/pages/NycOpenData311ServiceRequests"

type DatasetKey =
  | "nyc_open_data_311_service_requests"
  | "nyc_open_data_311_customer_satisfaction_survey"
  | "nyc_open_data_311_call_center_inquiry"

type PageProps = {
  docUrl: string
}

type DatasetDef = {
  key: DatasetKey
  label: string
  docUrl: string
  Component: (props: PageProps) => React.ReactNode
}

export default function Home() {
  const datasets = useMemo<DatasetDef[]>(
    () => [
      {
        key: "nyc_open_data_311_service_requests",
        label: "NYC 311 Service Requests",
        docUrl: "https://data.cityofnewyork.us/Social-Services/311-Service-Requests-for-2009/3rfa-3xsf/about_data",
        Component: NycOpenData311ServiceRequests,
      },
      {
        key: "nyc_open_data_311_customer_satisfaction_survey",
        label: "NYC 311 Customer Satisfaction Survey",
        docUrl: "https://data.cityofnewyork.us/City-Government/311-Customer-Satisfaction-Survey/kizp-4dfk/about_data",
        Component: NycOpenData311CustomerSatisfactionSurvey,
      },
      {
        key: "nyc_open_data_311_call_center_inquiry",
        label: "NYC 311 Call Center Inquiry",
        docUrl: "https://data.cityofnewyork.us/City-Government/311-Call-Center-Inquiry/wewp-mm3p/about_data",
        Component: NycOpenData311CallCenterInquiry,
      },
    ],
    []
  )

  const [selected, setSelected] = useState<DatasetKey>(
    datasets[0]?.key ?? "nyc_open_data_311_service_requests"
  )

  const sidebarDatasets = useMemo(
    () => datasets.map(({ key, label }) => ({ key, label, moniker: key })),
    [datasets]
  )

  const active = datasets.find((d) => d.key === selected) ?? datasets[0]
  const ActiveComponent = active?.Component

  return (
    <div className="h-full min-h-0 w-full rounded-lg border-0 border-[color:var(--color-accent)] [background:var(--color-bg)] p-3 text-[color:var(--color-text)]">
      <div className="flex h-full min-h-0 w-full gap-6">
        <DatasetSidebar datasets={sidebarDatasets} selected={selected} onSelect={setSelected} />
        <div className="min-h-0 min-w-0 flex-1">
          {ActiveComponent ? <ActiveComponent docUrl={active.docUrl} /> : null}
        </div>
      </div>
    </div>
  )
}
