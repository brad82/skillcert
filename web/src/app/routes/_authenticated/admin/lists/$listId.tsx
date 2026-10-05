import { createFileRoute } from '@tanstack/react-router'
import { competenciesQueryOptions } from '@features/admin-competencies'
import { ListEditorContainer, listQueryOptions } from '@features/admin-lists'

export const Route = createFileRoute('/_authenticated/admin/lists/$listId')({
  loader: ({ context, params }) =>
    Promise.all([
      context.queryClient.ensureQueryData(listQueryOptions(params.listId)),
      context.queryClient.ensureQueryData(competenciesQueryOptions()),
    ]),
  component: ListEditorRoute,
})

function ListEditorRoute() {
  const { listId } = Route.useParams()
  return <ListEditorContainer listId={listId} />
}
