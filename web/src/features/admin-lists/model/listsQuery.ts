import { queryOptions } from '@tanstack/react-query'
import { getGetListQueryKey, getList, getListListsQueryKey, listLists } from '../api/adminListsApi.gen'

/** Every competency list with its counts and groups. */
export const listsQueryOptions = () =>
  queryOptions({
    queryKey: getListListsQueryKey(),
    queryFn: ({ signal }) => listLists({ signal }),
    staleTime: 30_000,
  })

/** One list's whole tree. Every tree change returns the updated list, which replaces this entry. */
export const listQueryOptions = (listId: string) =>
  queryOptions({
    queryKey: getGetListQueryKey(listId),
    queryFn: ({ signal }) => getList(listId, { signal }),
    staleTime: 30_000,
  })
