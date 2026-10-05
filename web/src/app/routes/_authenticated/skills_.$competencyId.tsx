import { createFileRoute, notFound, useCanGoBack, useRouter } from '@tanstack/react-router'
import { CompetencyContainer, CompetencyNotFound, type CompetencyTab, isCompetencyTab } from '@features/competency'
import { myCompetencyQueryOptions } from '@features/my-record'
import { isApiError } from '@shared/api/client'

export type CompetencySearch = { tab?: CompetencyTab }

export const Route = createFileRoute('/_authenticated/skills_/$competencyId')({
  validateSearch: (search: Record<string, unknown>): CompetencySearch => ({
    tab: isCompetencyTab(search.tab) && search.tab !== 'overview' ? search.tab : undefined,
  }),
  loader: async ({ context, params }) => {
    try {
      await context.queryClient.ensureQueryData(myCompetencyQueryOptions(params.competencyId))
    } catch (error) {
      if (isApiError(error, 404)) throw notFound()
      throw error
    }
  },
  component: CompetencyRoute,
  notFoundComponent: NotFoundRoute,
})

/** Back keeps the skills list's filter and scroll when we came from it; otherwise opens the list. */
function useBackToSkills() {
  const router = useRouter()
  const canGoBack = useCanGoBack()
  return () => (canGoBack ? router.history.back() : router.navigate({ to: '/skills' }))
}

function CompetencyRoute() {
  const { competencyId } = Route.useParams()
  const { tab = 'overview' } = Route.useSearch()
  const navigate = Route.useNavigate()
  const onBack = useBackToSkills()
  return (
    <CompetencyContainer
      competencyId={competencyId}
      tab={tab}
      onTabChange={(next) => navigate({ search: { tab: next === 'overview' ? undefined : next }, replace: true })}
      onBack={onBack}
    />
  )
}

function NotFoundRoute() {
  return <CompetencyNotFound onBack={useBackToSkills()} />
}
