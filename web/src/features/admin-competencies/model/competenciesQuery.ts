import { queryOptions } from '@tanstack/react-query'
import { getListCompetenciesQueryKey, listCompetencies } from '../api/adminCompetenciesApi.gen'

/** Every competency, active and inactive, in code order. The list editor's picker reads it too. */
export const competenciesQueryOptions = () =>
  queryOptions({
    queryKey: getListCompetenciesQueryKey(),
    queryFn: ({ signal }) => listCompetencies(undefined, { signal }),
    staleTime: 30_000,
  })
