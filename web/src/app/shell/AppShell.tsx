import Box from '@mui/material/Box'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { Outlet, useNavigate } from '@tanstack/react-router'
import { useLogout } from '@features/auth'
import { CurrentUserProvider, currentUserQueryOptions } from '@features/current-user'
import { AppHeader } from './AppHeader'

const appShellStyles = () => ({
  page: { minHeight: '100dvh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' },
  main: { flexGrow: 1, p: { xs: 4, sm: 6 } },
})

/**
 * Signed-in chrome. Container for the shell: owns the current-user query (already loaded by the
 * route gate) and the sign-out mutation, then mounts CurrentUserProvider so every page below can
 * call useCurrentUser() without null checks.
 */
export function AppShell() {
  const { data: user } = useSuspenseQuery(currentUserQueryOptions())
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const logout = useLogout({
    mutation: {
      onSettled: async () => {
        queryClient.clear()
        await navigate({ to: '/login' })
      },
    },
  })
  const styles = appShellStyles()

  return (
    <CurrentUserProvider user={user}>
      <Box sx={styles.page}>
        <AppHeader signingOut={logout.isPending} onSignOut={() => logout.mutate()} />
        <Box component="main" sx={styles.main}>
          <Outlet />
        </Box>
      </Box>
    </CurrentUserProvider>
  )
}
