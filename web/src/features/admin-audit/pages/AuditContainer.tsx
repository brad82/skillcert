import { useSuspenseInfiniteQuery, useSuspenseQuery } from '@tanstack/react-query'
import { usersQueryOptions } from '@features/admin-users'
import { AuditProvider } from '../AuditProvider'
import { type AuditFilters, auditQueryOptions } from '../model/auditQuery'
import { AuditPage } from './AuditPage'

type Props = {
  filters: AuditFilters
  /** The URL holds the filters, so a user, list or competency can link straight to its history. */
  onFiltersChange: (filters: AuditFilters) => void
}

/** Owns the paged audit query (and the users query, for the Who filter). */
export function AuditContainer({ filters, onFiltersChange }: Props) {
  const audit = useSuspenseInfiniteQuery(auditQueryOptions(filters))
  const { data: users } = useSuspenseQuery(usersQueryOptions())
  const pages = audit.data.pages
  return (
    <AuditProvider
      entries={pages.flatMap((page) => page.entries)}
      entityTypes={pages[0]?.entityTypes ?? []}
      actors={users.users.filter((u) => u.isAdministrator).map((u) => ({ id: u.id, name: u.displayName }))}
      filters={filters}
      hasMore={audit.hasNextPage}
      loadingMore={audit.isFetchingNextPage}
      onLoadMore={() => void audit.fetchNextPage()}
      onFiltersChange={onFiltersChange}
    >
      <AuditPage />
    </AuditProvider>
  )
}
