"use client"

import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { useState } from "react"

import { DatasetSidebar } from "@/components/hbc/DatasetSidebar"

type K = "a" | "b"

function Harness() {
  const datasets = [
    { key: "a" as const, label: "Dataset A", moniker: "moniker_a" },
    { key: "b" as const, label: "Dataset B", moniker: "moniker_b" },
  ]
  const [selected, setSelected] = useState<K>("a")

  return (
    <DatasetSidebar
      datasets={datasets}
      selected={selected}
      onSelect={(k) => setSelected(k)}
    />
  )
}

test("shows title and reflects selected radio", () => {
  render(<Harness />)

  expect(screen.getByText(/datasets/i)).toBeInTheDocument()

  const a = screen.getByRole("radio", { name: /dataset a/i })
  const b = screen.getByRole("radio", { name: /dataset b/i })

  expect(a).toBeChecked()
  expect(b).not.toBeChecked()
})

test("clicking a dataset selects it", async () => {
  const user = userEvent.setup()
  render(<Harness />)

  const b = screen.getByRole("radio", { name: /dataset b/i })
  await user.click(b)

  expect(b).toBeChecked()
})
