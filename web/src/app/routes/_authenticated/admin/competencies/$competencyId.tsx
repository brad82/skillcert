import { createFileRoute } from '@tanstack/react-router'
import { CompetencyEditorContainer, competencyQueryOptions } from '@features/admin-competencies'

/** `listId`: the list the editor was opened from, for the breadcrumb back. */
export type CompetencySearch = { listId?: string }

export const Route = createFileRoute('/_authenticated/admin/competencies/$competencyId')({
  validateSearch: (search: Record<string, unknown>): CompetencySearch => (typeof search.listId === 'string' ? { listId: search.listId } : {}),
  loader: ({ context, params }) => context.queryClient.ensureQueryData(competencyQueryOptions(params.competencyId)),
  component: CompetencyEditorRoute,
})

function CompetencyEditorRoute() {
  const { competencyId } = Route.useParams()
  const { listId } = Route.useSearch()
  return <CompetencyEditorContainer key={competencyId} competencyId={competencyId} fromListId={listId} />
}
