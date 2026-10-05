import { createFileRoute } from '@tanstack/react-router'
import { HomeContainer } from '@features/home'
import { myListsQueryOptions } from '@features/my-record'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) => context.queryClient.ensureQueryData(myListsQueryOptions()),
  component: HomeContainer,
})
