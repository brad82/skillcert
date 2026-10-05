import { createFileRoute } from '@tanstack/react-router'
import { myArchivedRecordsQueryOptions, myListsQueryOptions } from '@features/my-record'
import { RecordsContainer } from '@features/records'

export const Route = createFileRoute('/_authenticated/records')({
  loader: ({ context }) =>
    Promise.all([
      context.queryClient.ensureQueryData(myListsQueryOptions()),
      context.queryClient.ensureQueryData(myArchivedRecordsQueryOptions()),
    ]),
  component: RecordsContainer,
})
