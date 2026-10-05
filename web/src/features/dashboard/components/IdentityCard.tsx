import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { Trans, useLingui } from '@lingui/react/macro'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { type Capability, useCurrentUser } from '@features/current-user'

const capabilityLabels: Record<Capability, MessageDescriptor> = {
  Administrator: msg`Administrator`,
  Instructor: msg`Instructor`,
  Supervisor: msg`Supervisor`,
}

const identityCardStyles = () => ({
  card: { maxWidth: 480, mx: 'auto' },
  chips: { mt: 4, flexWrap: 'wrap' },
})

/** Who is signed in, and what they can do. Every user is a candidate; other capabilities are additive. */
export function IdentityCard() {
  const { displayName, email, capabilities } = useCurrentUser()
  const { i18n } = useLingui()
  const styles = identityCardStyles()

  return (
    <Card variant="outlined" sx={styles.card}>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          <Trans>Signed in as</Trans>
        </Typography>
        <Typography variant="h1">
          {displayName}
        </Typography>
        <Typography color="text.secondary" gutterBottom>
          {email}
        </Typography>
        <Stack direction="row" spacing={2} sx={styles.chips} useFlexGap>
          <Chip label={<Trans>Candidate</Trans>} />
          {capabilities.map((capability) => {
            const label = capabilityLabels[capability as Capability]
            return <Chip key={capability} label={label ? i18n._(label) : capability} color="primary" />
          })}
        </Stack>
      </CardContent>
    </Card>
  )
}
