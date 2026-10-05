import { createFileRoute } from '@tanstack/react-router'
import { SignOffContainer, signOffReviewersQueryOptions } from '@features/sign-off'

export type SignOffSearch = { ids: string[] }

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export const Route = createFileRoute('/_authenticated/sign-off')({
  validateSearch: (search: Record<string, unknown>): SignOffSearch => ({
    ids: (Array.isArray(search.ids) ? search.ids : []).filter((id): id is string => typeof id === 'string' && uuid.test(id)).slice(0, 100),
  }),
  staticData: { fullScreen: true },
  loaderDeps: ({ search }) => ({ ids: search.ids }),
  loader: ({ context, deps }) =>
    deps.ids.length > 0 ? context.queryClient.ensureQueryData(signOffReviewersQueryOptions(deps.ids)) : undefined,
  component: SignOffRoute,
})

function SignOffRoute() {
  const { ids } = Route.useSearch()
  const navigate = Route.useNavigate()
  const toBasket = () => navigate({ to: '/basket', replace: true })
  return <SignOffContainer competencyIds={ids} onCancel={toBasket} onDone={toBasket} />
}
