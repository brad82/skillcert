import { queryOptions } from '@tanstack/react-query'
import type { z } from 'zod'
import type { GetCurrentUserResponse } from '@shared/schemas/skillcert.zod.gen'
import { getCurrentUser, getGetCurrentUserQueryKey } from '../api/currentUserApi.gen'

export type CurrentUser = z.infer<typeof GetCurrentUserResponse>

/** Additive capabilities the API reports (spec §18). Every signed-in user is also a candidate. */
export type Capability = 'Administrator' | 'Instructor' | 'Supervisor'

/**
 * The signed-in user. A 401/403 from this query means "not signed in" — the router gate
 * redirects on it. Shared by the gate (ensureQueryData) and the shell container.
 */
export const currentUserQueryOptions = () =>
  queryOptions({
    queryKey: getGetCurrentUserQueryKey(),
    queryFn: ({ signal }) => getCurrentUser({ signal }),
    retry: false,
    staleTime: 60_000,
  })
