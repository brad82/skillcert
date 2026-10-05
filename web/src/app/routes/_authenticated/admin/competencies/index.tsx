import { createFileRoute } from '@tanstack/react-router'
import { CompetenciesContainer, competenciesQueryOptions } from '@features/admin-competencies'

export const Route = createFileRoute('/_authenticated/admin/competencies/')({
  loader: ({ context }) => context.queryClient.ensureQueryData(competenciesQueryOptions()),
  component: CompetenciesContainer,
})
