import { createFileRoute } from '@tanstack/react-router'
import { listsQueryOptions } from '@features/admin-lists'
import { ImportContainer } from '@features/admin-import'

export const Route = createFileRoute('/_authenticated/admin/import')({
  loader: ({ context }) => context.queryClient.ensureQueryData(listsQueryOptions()),
  component: ImportContainer,
})
