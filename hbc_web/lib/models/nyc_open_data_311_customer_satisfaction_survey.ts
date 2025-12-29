import { z } from "zod"

// Cross-referenced with `hbc_configs/nyc_open_data_311_customer_satisfaction_survey.yaml`
export const NYC_OPEN_DATA_311_CUSTOMER_SATISFACTION_SURVEY_COLUMN_TYPES = {
  year: "text",
  campaign: "text",
  channel: "text",
  survey_type: "text",
  start_time: "datetime",
  completion_time: "datetime",
  survey_language: "text",
  overall_satisfaction: "text",
  wait_time: "text",
  agent_customer_service: "text",
  agent_job_knowledge: "text",
  answer_satisfaction: "text",
  nps: "number",
} as const satisfies Record<string, "text" | "number" | "date" | "datetime">

const nullableString = z.string().trim().min(1).nullable().optional()

const nullableNumber = z.preprocess((v) => {
  if (v === "" || v === null || v === undefined) return null
  if (typeof v === "number") return Number.isFinite(v) ? v : null
  if (typeof v === "string") {
    const n = Number(v)
    return Number.isFinite(n) ? n : null
  }
  return null
}, z.number().nullable().optional())

const nullableDateTimeString = z.preprocess((v) => {
  if (v === "" || v === null || v === undefined) return null
  if (typeof v !== "string") return null
  const s = v.trim()
  if (!s) return null
  const isoish = s.includes(" ") ? s.replace(" ", "T") : s
  const ms = Date.parse(isoish)
  return Number.isNaN(ms) ? s : isoish
}, z.string().nullable().optional())

export const NycOpenData311CustomerSatisfactionSurveySchema = z
  .object({
    hbc_unique_key: z.string().min(1).optional(),

    year: nullableString,
    campaign: nullableString,
    channel: nullableString,
    survey_type: nullableString,
    start_time: nullableDateTimeString,
    completion_time: nullableDateTimeString,
    survey_language: nullableString,
    overall_satisfaction: nullableString,
    wait_time: nullableString,
    agent_customer_service: nullableString,
    agent_job_knowledge: nullableString,
    answer_satisfaction: nullableString,
    nps: nullableNumber,
  })
  .passthrough()

export type NycOpenData311CustomerSatisfactionSurvey = z.infer<
  typeof NycOpenData311CustomerSatisfactionSurveySchema
>

export const NYC_OPEN_DATA_311_CUSTOMER_SATISFACTION_SURVEY_COLUMNS = [
  "year",
  "campaign",
  "channel",
  "survey_type",
  "start_time",
  "completion_time",
  "survey_language",
  "overall_satisfaction",
  "wait_time",
  "agent_customer_service",
  "agent_job_knowledge",
  "answer_satisfaction",
  "nps",
] as const satisfies ReadonlyArray<string>

export function parseNycOpenData311CustomerSatisfactionSurvey(input: unknown): {
  rows: NycOpenData311CustomerSatisfactionSurvey[]
  invalidCount: number
  issues: string[]
} {
  const arr = z.array(z.unknown()).safeParse(input)
  if (!arr.success) {
    return {
      rows: [],
      invalidCount: 0,
      issues: ["Response was not an array of rows."],
    }
  }

  const rows: NycOpenData311CustomerSatisfactionSurvey[] = []
  const issues: string[] = []
  let invalidCount = 0

  for (let i = 0; i < arr.data.length; i++) {
    const parsed = NycOpenData311CustomerSatisfactionSurveySchema.safeParse(arr.data[i])
    if (!parsed.success) {
      invalidCount++
      if (issues.length < 10) {
        issues.push(
          `Row ${i + 1}: ${parsed.error.issues
            .map((x) => `${x.path.join(".") || "(root)"} ${x.message}`)
            .join("; ")}`
        )
      }
      continue
    }
    rows.push(parsed.data)
  }

  if (invalidCount > issues.length) {
    issues.push(`…and ${invalidCount - issues.length} more invalid row(s).`)
  }

  return { rows, invalidCount, issues }
}
