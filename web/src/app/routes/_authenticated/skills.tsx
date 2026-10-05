import { createFileRoute } from '@tanstack/react-router'
import { myListsQueryOptions } from '@features/my-record'
import { SkillsContainer } from '@features/skills'
import type { PresentationState } from '@shared/lib/competencyStatus'

export type SkillsSearch = { status?: PresentationState }

const states: PresentationState[] = ['current', 'expiringSoon', 'expired', 'notCompetent', 'pending', 'notCertified']

export const Route = createFileRoute('/_authenticated/skills')({
  validateSearch: (search: Record<string, unknown>): SkillsSearch => ({
    status: states.includes(search.status as PresentationState) ? (search.status as PresentationState) : undefined,
  }),
  loader: ({ context }) => context.queryClient.ensureQueryData(myListsQueryOptions()),
  component: SkillsRoute,
})

function SkillsRoute() {
  const { status } = Route.useSearch()
  const navigate = Route.useNavigate()
  return (
    <SkillsContainer
      status={status}
      onClearStatus={() => navigate({ search: {}, replace: true })}
      onOpenCompetency={(competencyId) => navigate({ to: '/skills/$competencyId', params: { competencyId } })}
      onOpenBasket={() => navigate({ to: '/basket' })}
    />
  )
}
