export function normalizeIsoishDateTime(text: string) {
  const trimmed = text.trim()
  if (!trimmed) return null
  const normalized = trimmed.includes(" ") && !trimmed.includes("T") ? trimmed.replace(" ", "T") : trimmed
  const isoish = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,3})?)?(Z|[+-]\d{2}:\d{2})?$/
  return isoish.test(normalized) ? normalized : null
}

export function dateOnlyFromText(text: string) {
  const isoMatch = /^(\d{4}-\d{2}-\d{2})/.exec(text)
  if (isoMatch) return isoMatch[1]

  const ms = Date.parse(text)
  if (!Number.isFinite(ms)) return null
  const d = new Date(ms)

  const yyyy = String(d.getUTCFullYear()).padStart(4, "0")
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0")
  const dd = String(d.getUTCDate()).padStart(2, "0")
  return `${yyyy}-${mm}-${dd}`
}

export function formatDateOnly(value: unknown) {
  if (value === null || value === undefined) return ""
  if (value instanceof Date && Number.isFinite(value.getTime())) return value.toISOString().slice(0, 10)
  const s = String(value).trim()
  if (!s) return ""
  return dateOnlyFromText(s) ?? s
}

export function formatDateTime(value: unknown) {
  if (value === null || value === undefined) return ""
  if (value instanceof Date && Number.isFinite(value.getTime())) {
    const iso = value.toISOString()
    return `${iso.slice(0, 10)} ${iso.slice(11, 19)}`
  }

  const s = String(value).trim()
  if (!s) return ""

  const iso = normalizeIsoishDateTime(s)
  if (!iso) return s

  const date = iso.slice(0, 10)
  const time = iso.includes("T") ? iso.slice(11, 19) : ""
  return time ? `${date} ${time}` : date
}

