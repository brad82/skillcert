import { queryOptions } from '@tanstack/react-query'
import { getListUsersQueryKey, listUsers } from '../api/adminUsersApi.gen'

/** Every user, ordered by name, plus the assignable classifications. Invalidate after each change. */
export const usersQueryOptions = () =>
  queryOptions({
    queryKey: getListUsersQueryKey(),
    queryFn: ({ signal }) => listUsers(undefined, { signal }),
    staleTime: 30_000,
  })
