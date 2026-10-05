import { createFileRoute } from '@tanstack/react-router'
import { approvalsQueryOptions } from '@features/approvals'
import { HomeContainer } from '@features/home'
import { myListsQueryOptions } from '@features/my-record'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) =>
    Promise.all([
      context.queryClient.ensureQueryData(myListsQueryOptions()),
      context.queryClient.ensureQueryData(approvalsQueryOptions()),
    ]),
  component: HomeRoute,
})

function HomeRoute() {
  const navigate = Route.useNavigate()
  return <HomeContainer onOpenApprovals={() => navigate({ to: '/approvals' })} />
}
