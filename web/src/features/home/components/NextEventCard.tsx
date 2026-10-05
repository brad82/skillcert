import { Trans } from '@lingui/react/macro'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Typography from '@mui/material/Typography'

/** Placeholder until opportunities exist (Phase 6). */
export function NextEventCard() {
  return (
    <Card>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          <Trans>Your next event</Trans>
        </Typography>
        <Typography variant="body2" color="text.secondary">
          <Trans>Training events are coming soon.</Trans>
        </Typography>
      </CardContent>
    </Card>
  )
}
