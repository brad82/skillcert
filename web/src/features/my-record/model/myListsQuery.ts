import { queryOptions } from '@tanstack/react-query'
import { getGetMyListsQueryKey, getMyLists } from '../api/myRecordApi.gen'

/** The candidate's required lists with currency. Invalidate after any sign-off or confirmation. */
export const myListsQueryOptions = () =>
  queryOptions({
    queryKey: getGetMyListsQueryKey(),
    queryFn: ({ signal }) => getMyLists({ signal }),
    staleTime: 30_000,
  })
