import { Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Download } from 'lucide-react'
import { ComplianceCard } from '@shared/components/ComplianceCard'
import { useCurrentRecords } from '../RecordsProvider'

/** A card per required list: compliance, progress and "Download current record (PDF)" (wireframe 7a). */
export function CurrentRecords() {
  const records = useCurrentRecords()

  if (records.length === 0) {
    return (
      <Typography color="text.secondary">
        <Trans>You have no required skills records yet.</Trans>
      </Typography>
    )
  }

  return (
    <Stack spacing={4}>
      {records.map(({ list, downloadUrl }) => (
        <ComplianceCard
          key={list.id}
          list={list}
          action={
            <Button variant="outlined" fullWidth href={downloadUrl} download startIcon={<Download size={18} aria-hidden />}>
              <Trans>Download current record (PDF)</Trans>
            </Button>
          }
        />
      ))}
    </Stack>
  )
}
