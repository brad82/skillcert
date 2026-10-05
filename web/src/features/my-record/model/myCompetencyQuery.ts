import { queryOptions } from '@tanstack/react-query'
import { getGetMyCompetencyQueryKey, getGetMySignatureUrl, getMyCompetency } from '../api/myRecordApi.gen'

/** One required competency for the candidate (detail screen). Invalidate with the lists after any sign-off. */
export const myCompetencyQueryOptions = (competencyId: string) =>
  queryOptions({
    queryKey: getGetMyCompetencyQueryKey(competencyId),
    queryFn: ({ signal }) => getMyCompetency(competencyId, { signal }),
    staleTime: 30_000,
  })

/** Image URL for a signature on one of the candidate's own reviews (served as SVG, same-origin cookie). */
export const signatureUrl = (signatureId: string) => getGetMySignatureUrl(signatureId)
