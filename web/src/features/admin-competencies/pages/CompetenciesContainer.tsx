import { useSuspenseQuery } from '@tanstack/react-query'
import { CompetenciesProvider } from '../CompetenciesProvider'
import { competenciesQueryOptions } from '../model/competenciesQuery'
import { CompetenciesPage } from './CompetenciesPage'

/** Owns the library query. All competencies is read-only; rows open the editor. */
export function CompetenciesContainer() {
  const { data } = useSuspenseQuery(competenciesQueryOptions())
  return (
    <CompetenciesProvider competencies={data.competencies}>
      <CompetenciesPage />
    </CompetenciesProvider>
  )
}
