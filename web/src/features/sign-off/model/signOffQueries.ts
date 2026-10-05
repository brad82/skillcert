import { queryOptions } from '@tanstack/react-query'
import { getGetSignOffReviewersQueryKey, getSignOffReviewers } from '../api/signOffApi.gen'

/** Everyone who could sign all of these skills for the signed-in candidate, and how. */
export const signOffReviewersQueryOptions = (competencyIds: string[]) =>
  queryOptions({
    queryKey: getGetSignOffReviewersQueryKey({ competencyId: competencyIds }),
    queryFn: ({ signal }) => getSignOffReviewers({ competencyId: competencyIds }, { signal }),
    staleTime: 60_000,
  })
