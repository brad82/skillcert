import { useSuspenseInfiniteQuery } from '@tanstack/react-query'
import { AuditProvider } from '../AuditProvider'
import { type AuditFilters, auditQueryOptions } from '../model/auditQuery'
import { AuditPage } from './AuditPage'

type Props = {
  filters: AuditFilters
  /** The URL holds the filters, so a user, list or competency can link straight to its history. */
  onFiltersChange: (filters: AuditFilters) => void
}

/** Owns the paged audit query. The Who filter lists everyone in the log, former administrators included. */
export function AuditContainer({ filters, onFiltersChange }: Props) {
  const audit = useSuspenseInfiniteQuery(auditQueryOptions(filters))
  const pages = audit.data.pages
  return (
    <AuditProvider
      entries={pages.flatMap((page) => page.entries)}
      entityTypes={pages[0]?.entityTypes ?? []}
      actors={pages[0]?.actors ?? []}
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
