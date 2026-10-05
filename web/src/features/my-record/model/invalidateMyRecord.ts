import type { QueryClient } from '@tanstack/react-query'

/** Refetch everything under /api/me (lists, competency detail) after a sign-off or a confirmation. */
export const invalidateMyRecord = (queryClient: QueryClient) =>
  queryClient.invalidateQueries({ predicate: (query) => String(query.queryKey[0]).startsWith('/api/me/') })
