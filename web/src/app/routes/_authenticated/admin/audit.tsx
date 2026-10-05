import { createFileRoute } from '@tanstack/react-router'
import { type AuditFilters, AuditContainer, auditQueryOptions } from '@features/admin-audit'
import { usersQueryOptions } from '@features/admin-users'

const keys = ['entityType', 'entityId', 'actorUserId', 'from', 'to'] as const

export const Route = createFileRoute('/_authenticated/admin/audit')({
  validateSearch: (search: Record<string, unknown>): AuditFilters =>
    Object.fromEntries(keys.flatMap((key) => (typeof search[key] === 'string' && search[key] !== '' ? [[key, search[key]]] : []))),
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) =>
    Promise.all([context.queryClient.ensureInfiniteQueryData(auditQueryOptions(deps)), context.queryClient.ensureQueryData(usersQueryOptions())]),
  component: AuditRoute,
})

function AuditRoute() {
  const filters = Route.useSearch()
  const navigate = Route.useNavigate()
  return <AuditContainer filters={filters} onFiltersChange={(next) => navigate({ search: next, replace: true })} />
}
