import AppBar from '@mui/material/AppBar'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { AccountMenu } from './AccountMenu'

type Props = {
  signingOut: boolean
  onSignOut: () => void
}

const appHeaderStyles = () => ({
  bar: { pt: 'env(safe-area-inset-top, 0px)' },
  title: { flexGrow: 1 },
})

export function AppHeader({ signingOut, onSignOut }: Props) {
  const styles = appHeaderStyles()

  return (
    <AppBar position="sticky" sx={styles.bar}>
      <Toolbar>
        <Typography variant="h3" component="span" sx={styles.title}>
          SkillCert
        </Typography>
        <AccountMenu signingOut={signingOut} onSignOut={onSignOut} />
      </Toolbar>
    </AppBar>
  )
}
