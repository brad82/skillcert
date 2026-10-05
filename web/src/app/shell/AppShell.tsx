import Box from '@mui/material/Box'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { Outlet, useNavigate } from '@tanstack/react-router'
import { useLogout } from '@features/auth'
import { BasketProvider } from '@features/basket'
import { CurrentUserProvider, currentUserQueryOptions } from '@features/current-user'
import { AppHeader } from './AppHeader'
import { BottomNav } from './BottomNav'

const appShellStyles = () => ({
  page: { minHeight: '100dvh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' },
  // Clears the fixed bottom nav (56px) plus the phone's home-indicator inset.
  main: { flexGrow: 1, px: { xs: 4, sm: 6 }, pt: { xs: 4, sm: 6 }, pb: 'calc(56px + 24px + env(safe-area-inset-bottom, 0px))', width: '100%', maxWidth: 720, mx: 'auto' },
})

/**
 * Signed-in chrome. Container for the shell: owns the current-user query (already loaded by the
 * route gate) and the sign-out mutation, then mounts CurrentUserProvider (so useCurrentUser() is never
 * null below) and the basket, and draws the app bar and the five-tab bottom nav.
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
      <BasketProvider userId={user.id}>
        <Box sx={styles.page}>
          <AppHeader signingOut={logout.isPending} onSignOut={() => logout.mutate()} />
          <Box component="main" sx={styles.main}>
            <Outlet />
          </Box>
          <BottomNav />
        </Box>
      </BasketProvider>
    </CurrentUserProvider>
  )
}
