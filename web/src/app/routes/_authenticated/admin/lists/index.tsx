import { createFileRoute } from '@tanstack/react-router'
import { ListsContainer, listsQueryOptions } from '@features/admin-lists'

export const Route = createFileRoute('/_authenticated/admin/lists/')({
  loader: ({ context }) => context.queryClient.ensureQueryData(listsQueryOptions()),
  component: ListsContainer,
})
