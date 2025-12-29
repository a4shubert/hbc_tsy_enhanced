// components/DatasetDashboard.tsx
"use client"

import { useMemo, useState } from "react"

import { DatasetSidebar } from "@/components/DatasetSidebar"
import NycOpenData311CallCenterInquiry from "@/components/pages/NycOpenData311CallCenterInquiry"
import NycOpenData311CustomerSatisfactionSurvey from "@/components/pages/NycOpenData311CustomerSatisfactionSurvey"
import NycOpenData311ServiceRequests from "@/components/pages/NycOpenData311ServiceRequests"

import type { DatasetDef, DatasetKey } from "@/components/DatasetDashboard.types"

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

  const active = datasets.find((d) => d.key === selected) ?? datasets[0]

  return (
    <div className="flex h-full min-h-0 w-full gap-6">
      <DatasetSidebar datasets={datasets} selected={selected} onSelect={setSelected} />

      <div className="min-h-0 min-w-0 flex-1">
        {active?.render()}
      </div>
    </div>
  )
}
