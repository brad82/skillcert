const dateFormats = new Map<string, Intl.DateTimeFormat>()

/** "9 Nov 2026" in the active locale. Dates come from the API as ISO-8601 instants. */
export function formatDate(iso: string, locale: string): string {
  let format = dateFormats.get(locale)
  if (!format) {
    format = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric' })
    dateFormats.set(locale, format)
  }
  return format.format(new Date(iso))
}

/** Whole days from now until the instant (negative when past). */
export function daysUntil(iso: string, now: Date = new Date()): number {
  return Math.ceil((new Date(iso).getTime() - now.getTime()) / 86_400_000)
}
