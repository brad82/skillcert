import { createContext, type ReactNode, useContext, useState } from 'react'
import type { AuditEntryDto } from '@shared/api/model'
import type { AuditFilters } from './model/auditQuery'

type Props = {
  entries: AuditEntryDto[]
  entityTypes: string[]
  /** Administrators, for the Who filter. */
  actors: { id: string; name: string }[]
  filters: AuditFilters
  hasMore: boolean
  loadingMore: boolean
  onLoadMore: () => void
  onFiltersChange: (filters: AuditFilters) => void
  children: ReactNode
}

type AuditContextValue = Omit<Props, 'children'> & { expanded: ReadonlySet<string>; toggle: (id: string) => void }

const AuditContext = createContext<AuditContextValue | null>(null)

/** The audit log's expanded rows, composed with the entries and filters passed in. No data-layer dependency. */
export function AuditProvider({ children, ...props }: Props) {
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set())
  const toggle = (id: string) =>
    setExpanded((current) => {
      const next = new Set(current)
      if (!next.delete(id)) next.add(id)
      return next
    })
  return <AuditContext.Provider value={{ ...props, expanded, toggle }}>{children}</AuditContext.Provider>
}

function useAuditContext() {
  const context = useContext(AuditContext)
  if (!context) throw new Error('Audit hooks must be used within an AuditProvider')
  return context
}

export function useAuditFilters() {
  const { filters, onFiltersChange, entityTypes, actors, entries } = useAuditContext()
  const entityLabel = filters.entityId ? (entries.find((e) => e.entityId === filters.entityId)?.entityLabel ?? null) : null
  return {
    filters,
    entityTypes,
    actors,
    entityLabel,
    set: (patch: AuditFilters) => onFiltersChange({ ...filters, ...patch }),
    clear: () => onFiltersChange({}),
  }
}

export function useAuditEntries() {
  const { entries, expanded, toggle, hasMore, loadingMore, onLoadMore } = useAuditContext()
  return { entries, expanded, toggle, hasMore, loadingMore, loadMore: onLoadMore }
}
