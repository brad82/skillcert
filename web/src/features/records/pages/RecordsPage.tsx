import { Trans } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { ArchivedRecords } from '../components/ArchivedRecords'
import { CurrentRecords } from '../components/CurrentRecords'

/** Wireframe 7a: current records with downloads, then archived records. Layout only. */
export function RecordsPage() {
  return (
    <Stack spacing={6}>
      <Typography variant="h1">
        <Trans>Records</Trans>
      </Typography>
      <CurrentRecords />
      <ArchivedRecords />
    </Stack>
  )
}
