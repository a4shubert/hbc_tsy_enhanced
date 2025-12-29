import { DatasetDashboard } from "@/components/DatasetDashboard"

export default function Home() {
  return (
    <div className="h-full min-h-0 w-full rounded-lg border-0 border-[color:var(--color-accent)] [background:var(--color-bg)] p-3 text-[color:var(--color-text)]">
      <DatasetDashboard />
    </div>
  )
}
