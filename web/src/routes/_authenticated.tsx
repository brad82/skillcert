import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { Outlet, createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import { ApiError } from '../api/client'
import { useLogout } from '../api/generated/auth/auth'
import { currentUserQueryOptions } from '../auth'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context, location }) => {
    try {
      await context.queryClient.ensureQueryData(currentUserQueryOptions())
    } catch (error) {
      if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
        throw redirect({ to: '/login', search: { redirect: location.href } })
      }
      throw error
    }
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  const { data: me } = useSuspenseQuery(currentUserQueryOptions())
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

  return (
    <Box sx={{ minHeight: '100dvh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="sticky" elevation={0}>
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" component="span" sx={{ flexGrow: 1 }}>
            SkillCert
          </Typography>
          <Typography variant="body2" sx={{ display: { xs: 'none', sm: 'block' } }}>
            {me.displayName}
          </Typography>
          <Button color="inherit" onClick={() => logout.mutate()} disabled={logout.isPending}>
            Log out
          </Button>
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flexGrow: 1, p: { xs: 2, sm: 3 } }}>
        <Outlet />
      </Box>
    </Box>
  )
}
