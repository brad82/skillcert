import { createFileRoute, redirect } from '@tanstack/react-router'
import { currentUserQueryOptions } from '@features/current-user'
import { AdminShell } from '../../shell/admin/AdminShell'

/** Admin gate: administrators only (the API answers 403 to anyone else). Draws its own chrome. */
export const Route = createFileRoute('/_authenticated/admin')({
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(currentUserQueryOptions())
    if (!user.capabilities.includes('Administrator')) throw redirect({ to: '/' })
  },
  staticData: { fullScreen: true },
  component: AdminShell,
})
