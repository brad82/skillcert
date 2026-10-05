import { createFileRoute, redirect } from '@tanstack/react-router'
import { currentUserQueryOptions } from '@features/current-user'
import { isApiError } from '@shared/api/client'
import { AppShell } from '../shell/AppShell'

/** Auth gate: every route under here needs a signed-in, active user. */
export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context, location }) => {
    try {
      await context.queryClient.ensureQueryData(currentUserQueryOptions())
    } catch (error) {
      if (isApiError(error, 401, 403)) {
        throw redirect({ to: '/login', search: { redirect: location.href } })
      }
      throw error
    }
  },
  component: AppShell,
})
