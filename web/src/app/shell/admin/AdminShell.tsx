import { Trans, useLingui } from '@lingui/react/macro'
import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Drawer from '@mui/material/Drawer'
import IconButton from '@mui/material/IconButton'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useQueryClient } from '@tanstack/react-query'
import { Outlet, useNavigate } from '@tanstack/react-router'
import { ButtonLink } from '@shared/components/RouterLinks'
import { Menu } from 'lucide-react'
import { useState } from 'react'
import { useLogout } from '@features/auth'
import { AccountMenu } from '../AccountMenu'
import { AdminNav } from './AdminNav'

const drawerWidth = 240

const adminShellStyles = () => ({
  page: { display: 'flex', minHeight: '100dvh', bgcolor: 'background.default' },
  docked: { display: { xs: 'none', md: 'block' }, width: drawerWidth, flexShrink: 0, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' } },
  temporary: { display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: 'min(304px, 85vw)', boxSizing: 'border-box' } },
  column: { flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' },
  menuButton: { display: { md: 'none' }, ml: -2 },
  title: { display: { md: 'none' } },
  spacer: { flexGrow: 1 },
  main: { flexGrow: 1, px: { xs: 4, sm: 6 }, py: { xs: 4, sm: 8 }, width: '100%', maxWidth: 1280, boxSizing: 'border-box' },
})

/**
 * Admin chrome: MUI's responsive drawer. One menu (AdminNav) is docked full height from md up and slides in
 * from the left on phones, opened by the app bar's menu button. The route gate has already checked the user
 * is an administrator; this container owns only the sign-out mutation and the phone drawer's open state.
 */
export function AdminShell() {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const logout = useLogout({
    mutation: {
      onSettled: async () => {
        queryClient.clear()
        await navigate({ to: '/login' })
      },
    },
  })
  const styles = adminShellStyles()

  return (
    <Box sx={styles.page}>
      <Drawer variant="permanent" sx={styles.docked} open>
        <AdminNav />
      </Drawer>
      <Drawer variant="temporary" open={open} onClose={() => setOpen(false)} sx={styles.temporary} ModalProps={{ keepMounted: true }}>
        <AdminNav onNavigate={() => setOpen(false)} />
      </Drawer>
      <Box sx={styles.column}>
        <AppBar position="sticky">
          <Toolbar>
            <IconButton color="inherit" aria-label={t`Open menu`} onClick={() => setOpen(true)} sx={styles.menuButton}>
              <Menu size={24} aria-hidden />
            </IconButton>
            <Typography variant="h3" component="span" sx={styles.title}>
              SkillCert
            </Typography>
            <Box sx={styles.spacer} />
            <ButtonLink color="inherit" to="/">
              <Trans>My record</Trans>
            </ButtonLink>
            <AccountMenu signingOut={logout.isPending} onSignOut={() => logout.mutate()} />
          </Toolbar>
        </AppBar>
        <Box component="main" sx={styles.main}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}
