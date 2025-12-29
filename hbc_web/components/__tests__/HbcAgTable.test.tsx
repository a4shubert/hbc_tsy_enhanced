"use client"

import { render, screen } from "@testing-library/react"

import { HbcAgTable } from "@/components/hbc/HbcAgTable"

jest.mock("ag-grid-react", () => ({
  AgGridReact: () => <div data-testid="ag-grid-react" />,
}))

test("renders error message", () => {
  render(<HbcAgTable rowData={[]} error="Boom" />)
  expect(screen.getByText("Boom")).toBeInTheDocument()
})

test("applies height style to wrapper", () => {
  const { container } = render(<HbcAgTable rowData={[]} height="123px" />)
  const wrapper = container.querySelector("div.w-full")
  expect(wrapper).toHaveStyle({ height: "123px" })
})
