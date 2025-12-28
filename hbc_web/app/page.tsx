import NycOpenData311CallCenterInquiry from "@/components/pages/NycOpenData311CallCenterInquiry"
import NycOpenData311CustomerSatisfactionSurvey from "@/components/pages/NycOpenData311CustomerSatisfactionSurvey"
import NycOpenData311ServiceRequests from "@/components/pages/NycOpenData311ServiceRequests"

export default function Home() {
  return (
    <div className="w-full rounded-lg border-0 border-[color:var(--color-accent)] bg-[color:var(--color-bg)] p-3 text-[color:var(--color-text)]">
      <div className="flex flex-col gap-6">
        <NycOpenData311ServiceRequests />
        <NycOpenData311CustomerSatisfactionSurvey />
        <NycOpenData311CallCenterInquiry />
      </div>
    </div>
  )
}