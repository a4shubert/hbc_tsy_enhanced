import NycOpenData311ServiceRequests from "@/components/pages/NycOpenData311ServiceRequests"

export default function Home() {
  return (
    <div className="w-full rounded-lg border-2 border-[color:var(--color-accent)] bg-[color:var(--color-bg)] p-3 text-[color:var(--color-text)]">
      <NycOpenData311ServiceRequests />
    </div>
  )
}
