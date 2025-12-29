import type { ReactNode } from "react"

export type DatasetKey = "service_requests" | "customer_satisfaction_survey" | "call_center_inquiry"

export type DatasetDef = {
  key: DatasetKey
  label: string
  moniker: string
  render: () => ReactNode
}

