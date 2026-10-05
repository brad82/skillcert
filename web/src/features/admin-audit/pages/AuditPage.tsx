import { Trans } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { AuditFiltersBar } from '../components/AuditFiltersBar'
import { AuditTable } from '../components/AuditTable'

/** Audit log: filters and the entries. Layout only. */
export function AuditPage() {
  return (
    <Stack spacing={6}>
      <div>
        <Typography variant="h1"><Trans>Audit log</Trans></Typography>
        <Typography variant="body2" color="text.secondary"><Trans>Every admin change: who, when, what, and the values before and after. Newest first.</Trans></Typography>
      </div>
      <AuditFiltersBar />
      <AuditTable />
    </Stack>
  )
}
