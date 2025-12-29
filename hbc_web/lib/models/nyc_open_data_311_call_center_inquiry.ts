import { z } from "zod"

// Cross-referenced with `hbc_configs/nyc_open_data_311_call_center_inquiry.yaml`
export const NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMN_TYPES = {
  unique_id: "text",
  date: "date",
  time: "text",
  date_time: "datetime",
  agency: "text",
  agency_name: "text",
  inquiry_name: "text",
  brief_description: "text",
  call_resolution: "text",
} as const satisfies Record<string, "text" | "number" | "date" | "datetime">

const nullableString = z.string().trim().min(1).nullable().optional()

const nullableDateOnlyString = z.preprocess((v) => {
  if (v === "" || v === null || v === undefined) return null
  if (typeof v !== "string") return null
  const s = v.trim()
  if (!s) return null
  const ms = Date.parse(s.includes(" ") ? s.replace(" ", "T") : s)
  if (Number.isNaN(ms)) return s
  return new Date(ms).toISOString().slice(0, 10)
}, z.string().nullable().optional())

const nullableDateTimeString = z.preprocess((v) => {
  if (v === "" || v === null || v === undefined) return null
  if (typeof v !== "string") return null
  const s = v.trim()
  if (!s) return null
  const isoish = s.includes(" ") ? s.replace(" ", "T") : s
  const ms = Date.parse(isoish)
  return Number.isNaN(ms) ? s : isoish
}, z.string().nullable().optional())

export const NycOpenData311CallCenterInquirySchema = z
  .object({
    hbc_unique_key: z.string().min(1).optional(),

    unique_id: nullableString,
    date: nullableDateOnlyString,
    time: nullableString,
    date_time: nullableDateTimeString,
    agency: nullableString,
    agency_name: nullableString,
    inquiry_name: nullableString,
    brief_description: nullableString,
    call_resolution: nullableString,
  })
  .passthrough()

export type NycOpenData311CallCenterInquiry = z.infer<typeof NycOpenData311CallCenterInquirySchema>

export const NYC_OPEN_DATA_311_CALL_CENTER_INQUIRY_COLUMNS = [
  "unique_id",
  "date",
  "time",
  "date_time",
  "agency",
  "agency_name",
  "inquiry_name",
  "brief_description",
  "call_resolution",
] as const satisfies ReadonlyArray<string>

export function parseNycOpenData311CallCenterInquiry(input: unknown): {
  rows: NycOpenData311CallCenterInquiry[]
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

  const rows: NycOpenData311CallCenterInquiry[] = []
  const issues: string[] = []
  let invalidCount = 0

  for (let i = 0; i < arr.data.length; i++) {
    const parsed = NycOpenData311CallCenterInquirySchema.safeParse(arr.data[i])
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
