import { infiniteQueryOptions } from '@tanstack/react-query'
import type { ListAuditEntriesParams } from '@shared/api/model'
import { getListAuditEntriesQueryKey, listAuditEntries } from '../api/adminAuditApi.gen'

/** Filters as the URL holds them: dates are local calendar days (yyyy-mm-dd). */
export type AuditFilters = { entityType?: string; entityId?: string; actorUserId?: string; from?: string; to?: string }

/** Local midnight of a calendar day, as an instant; `to` covers its whole day. */
const startOfDay = (day: string, plusDays = 0) => {
  const date = new Date(`${day}T00:00:00`)
  date.setDate(date.getDate() + plusDays)
  return date.toISOString()
}

const toParams = (filters: AuditFilters): ListAuditEntriesParams => ({
  entityType: filters.entityType,
  entityId: filters.entityId,
  actorUserId: filters.actorUserId,
  from: filters.from ? startOfDay(filters.from) : undefined,
  to: filters.to ? startOfDay(filters.to, 1) : undefined,
})

/** The audit log, newest first, 50 per page; the next page passes `before` = the last entry's time. */
export const auditQueryOptions = (filters: AuditFilters) =>
  infiniteQueryOptions({
    queryKey: getListAuditEntriesQueryKey(toParams(filters)),
    queryFn: ({ pageParam, signal }) => listAuditEntries({ ...toParams(filters), before: pageParam }, { signal }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => (page.hasMore ? page.entries.at(-1)?.at : undefined),
    staleTime: 30_000,
  })
