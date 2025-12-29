import { z } from "zod"

// Cross-referenced with `hbc_configs/nyc_open_data_311_service_requests.yaml`
export const NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMN_TYPES = {
  address_type: "text",
  agency: "text",
  agency_name: "text",
  borough: "text",
  bridge_highway_direction: "text",
  bridge_highway_name: "text",
  bridge_highway_segment: "text",
  city: "text",
  closed_date: "datetime",
  community_board: "text",
  complaint_type: "text",
  created_date: "datetime",
  cross_street_1: "text",
  cross_street_2: "text",
  descriptor: "text",
  due_date: "datetime",
  facility_type: "text",
  ferry_direction: "text",
  ferry_terminal_name: "text",
  garage_lot_name: "text",
  incident_address: "text",
  incident_zip: "text",
  intersection_street_1: "text",
  intersection_street_2: "text",
  landmark: "text",
  latitude: "number",
  location: "text",
  location_type: "text",
  longitude: "number",
  park_borough: "text",
  park_facility_name: "text",
  resolution_action_updated_date: "datetime",
  road_ramp: "text",
  school_address: "text",
  school_city: "text",
  school_code: "text",
  school_name: "text",
  school_not_found: "text",
  school_number: "text",
  school_or_citywide_complaint: "text",
  school_phone_number: "text",
  school_region: "text",
  school_state: "text",
  school_zip: "text",
  status: "text",
  street_name: "text",
  taxi_company_borough: "text",
  taxi_pick_up_location: "text",
  unique_key: "text",
  vehicle_type: "text",
  x_coordinate_state_plane_: "number",
  y_coordinate_state_plane_: "number",
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

export const NycOpenData311ServiceRequestSchema = z
  .object({
    hbc_unique_key: z.string().min(1).optional(),
    unique_key: nullableString,

    address_type: nullableString,
    agency: nullableString,
    agency_name: nullableString,
    borough: nullableString,
    bridge_highway_direction: nullableString,
    bridge_highway_name: nullableString,
    bridge_highway_segment: nullableString,
    city: nullableString,
    closed_date: nullableDateTimeString,
    community_board: nullableString,
    complaint_type: nullableString,
    created_date: nullableDateTimeString,
    cross_street_1: nullableString,
    cross_street_2: nullableString,
    descriptor: nullableString,
    due_date: nullableDateTimeString,
    facility_type: nullableString,
    ferry_direction: nullableString,
    ferry_terminal_name: nullableString,
    garage_lot_name: nullableString,
    incident_address: nullableString,
    incident_zip: nullableString,
    intersection_street_1: nullableString,
    intersection_street_2: nullableString,
    landmark: nullableString,
    latitude: nullableNumber,
    location: nullableString,
    location_type: nullableString,
    longitude: nullableNumber,
    park_borough: nullableString,
    park_facility_name: nullableString,
    resolution_action_updated_date: nullableDateTimeString,
    road_ramp: nullableString,
    school_address: nullableString,
    school_city: nullableString,
    school_code: nullableString,
    school_name: nullableString,
    school_not_found: nullableString,
    school_number: nullableString,
    school_or_citywide_complaint: nullableString,
    school_phone_number: nullableString,
    school_region: nullableString,
    school_state: nullableString,
    school_zip: nullableString,
    status: nullableString,
    street_name: nullableString,
    taxi_company_borough: nullableString,
    taxi_pick_up_location: nullableString,
    vehicle_type: nullableString,
    x_coordinate_state_plane_: nullableNumber,
    y_coordinate_state_plane_: nullableNumber,
  })
  .passthrough()

export type NycOpenData311ServiceRequest = z.infer<typeof NycOpenData311ServiceRequestSchema>

export const NYC_OPEN_DATA_311_SERVICE_REQUESTS_COLUMNS = [
  "address_type",
  "agency",
  "agency_name",
  "borough",
  "bridge_highway_direction",
  "bridge_highway_name",
  "bridge_highway_segment",
  "city",
  "closed_date",
  "community_board",
  "complaint_type",
  "created_date",
  "cross_street_1",
  "cross_street_2",
  "descriptor",
  "due_date",
  "facility_type",
  "ferry_direction",
  "ferry_terminal_name",
  "garage_lot_name",
  "incident_address",
  "incident_zip",
  "intersection_street_1",
  "intersection_street_2",
  "landmark",
  "latitude",
  "location",
  "location_type",
  "longitude",
  "park_borough",
  "park_facility_name",
  "resolution_action_updated_date",
  "road_ramp",
  "school_address",
  "school_city",
  "school_code",
  "school_name",
  "school_not_found",
  "school_number",
  "school_or_citywide_complaint",
  "school_phone_number",
  "school_region",
  "school_state",
  "school_zip",
  "status",
  "street_name",
  "taxi_company_borough",
  "taxi_pick_up_location",
  "unique_key",
  "vehicle_type",
  "x_coordinate_state_plane_",
  "y_coordinate_state_plane_",
] as const satisfies ReadonlyArray<string>

export function parseNycOpenData311ServiceRequests(input: unknown): {
  rows: NycOpenData311ServiceRequest[]
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

  const rows: NycOpenData311ServiceRequest[] = []
  const issues: string[] = []
  let invalidCount = 0

  for (let i = 0; i < arr.data.length; i++) {
    const parsed = NycOpenData311ServiceRequestSchema.safeParse(arr.data[i])
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
