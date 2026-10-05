import { Trans } from '@lingui/react/macro'
import AppBar from '@mui/material/AppBar'
import Button from '@mui/material/Button'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useCurrentUser } from '@features/current-user'
import { LanguageSwitcher } from '@shared/components/LanguageSwitcher'
import { ThemeModeToggle } from '@shared/components/ThemeModeToggle'

type Props = {
  signingOut: boolean
  onSignOut: () => void
}

const appHeaderStyles = () => ({
  toolbar: { gap: 1 },
  title: { flexGrow: 1 },
  name: { display: { xs: 'none', sm: 'block' }, mx: 1 },
})

export function AppHeader({ signingOut, onSignOut }: Props) {
  const { displayName } = useCurrentUser()
  const styles = appHeaderStyles()

  return (
    <AppBar position="sticky" elevation={0}>
      <Toolbar sx={styles.toolbar}>
        <Typography variant="h6" component="span" sx={styles.title}>
          SkillCert
        </Typography>
        <Typography variant="body2" sx={styles.name}>
          {displayName}
        </Typography>
        <LanguageSwitcher />
        <ThemeModeToggle />
        <Button color="inherit" onClick={onSignOut} disabled={signingOut}>
          <Trans>Sign out</Trans>
        </Button>
      </Toolbar>
    </AppBar>
  )
}
