import type { JsonElement } from '@shared/api/model'

export type ChangeRow = { field: string; before: string; after: string; changed: boolean }

const show = (value: unknown) => (value === undefined ? '—' : typeof value === 'string' ? value : JSON.stringify(value))

const asObject = (value: JsonElement | null): Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value) ? (value as Record<string, unknown>) : value === null ? {} : { value }

/** One row per field in either snapshot, so before/after reads as a diff. */
export function changeRows(before: JsonElement | null, after: JsonElement | null): ChangeRow[] {
  const b = asObject(before)
  const a = asObject(after)
  return [...new Set([...Object.keys(b), ...Object.keys(a)])].map((field) => ({
    field,
    before: show(b[field]),
    after: show(a[field]),
    changed: JSON.stringify(b[field]) !== JSON.stringify(a[field]),
  }))
}
