import { z } from "zod"

const nullableString = z.string().trim().min(1).nullable().optional()

const nullableDateString = z.preprocess((v) => {
  if (v === "" || v === null || v === undefined) return null
  if (typeof v !== "string") return null
  const s = v.trim()
  if (!s) return null
  const isoish = s.includes(" ") ? s.replace(" ", "T") : s
  const ms = Date.parse(isoish)
  return Number.isNaN(ms) ? s : s
}, z.string().nullable().optional())

export const NycOpenData311CallCenterInquirySchema = z
  .object({
    hbc_unique_key: z.string().min(1).optional(),

    unique_id: nullableString,
    date: nullableDateString,
    time: nullableString,
    date_time: nullableDateString,
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
