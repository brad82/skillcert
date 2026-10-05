import { Trans } from '@lingui/react/macro'
import AppBar from '@mui/material/AppBar'
import Button from '@mui/material/Button'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useSignOffChrome } from '../SignOffProvider'

const reviewerBarStyles = () => ({
  bar: { pt: 'env(safe-area-inset-top, 0px)', bgcolor: '#111', color: '#fff' },
  title: { flexGrow: 1 },
  exit: { color: '#fff', fontWeight: 700 },
})

/** Reviewer mode's black app bar (wireframe 5a). EXIT hands the phone back and saves nothing. */
export function ReviewerBar() {
  const { exit } = useSignOffChrome()
  const styles = reviewerBarStyles()

  return (
    <AppBar position="sticky" elevation={0} sx={styles.bar}>
      <Toolbar>
        <Typography variant="h3" component="span" sx={styles.title}>
          <Trans>Reviewer mode</Trans>
        </Typography>
        <Button onClick={exit} sx={styles.exit}>
          <Trans>Exit</Trans>
        </Button>
      </Toolbar>
    </AppBar>
  )
}
