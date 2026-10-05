import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'

/** Shown when a route fails to load for a reason other than "not signed in". */
export function AppErrorScreen() {
  return (
    <Box sx={{ p: 6, maxWidth: 480, mx: 'auto' }}>
      <Alert
        severity="error"
        action={
          <Button color="inherit" size="small" onClick={() => window.location.reload()}>
            <Trans>Reload</Trans>
          </Button>
        }
      >
        <Trans>Something went wrong loading SkillCert.</Trans>
      </Alert>
    </Box>
  )
}
