import { queryOptions } from '@tanstack/react-query'
import { getCurrentUser, getGetCurrentUserQueryKey } from './api/generated/current-user/current-user'

/** The signed-in user. A 401 from this query means "not signed in". */
export const currentUserQueryOptions = () =>
  queryOptions({
    queryKey: getGetCurrentUserQueryKey(),
    queryFn: ({ signal }) => getCurrentUser({ signal }),
    retry: false,
    staleTime: 60_000,
  })
