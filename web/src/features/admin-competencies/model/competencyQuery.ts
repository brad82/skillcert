import { queryOptions } from '@tanstack/react-query'
import { getCompetency, getGetCompetencyQueryKey } from '../api/adminCompetenciesApi.gen'

/** One competency: its current revision, revision history and the lists that use it. */
export const competencyQueryOptions = (competencyId: string) =>
  queryOptions({
    queryKey: getGetCompetencyQueryKey(competencyId),
    queryFn: ({ signal }) => getCompetency(competencyId, { signal }),
    staleTime: 30_000,
  })
