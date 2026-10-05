import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { UserConfirmDialog } from '../components/UserConfirmDialog'
import { UserFilters } from '../components/UserFilters'
import { UserPanel } from '../components/UserPanel'
import { UsersTable } from '../components/UsersTable'
import { useUsersHeader } from '../UsersProvider'

/** Users admin: filters and table, with the selected user's panel beside it. Layout only. */
export function UsersPage() {
  const { error } = useUsersHeader()

  return (
    <Stack spacing={6}>
      <div>
        <Typography variant="h1"><Trans>Users</Trans></Typography>
        <Typography variant="body2" color="text.secondary"><Trans>Users are deactivated, never deleted. Their history stays.</Trans></Typography>
      </div>
      {error && <Alert severity="error">{error}</Alert>}
      <Stack direction="row" spacing={6} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'flex-start' }}>
        <Stack spacing={4} sx={{ flex: '999 1 560px', minWidth: 0 }}>
          <UserFilters />
          <UsersTable />
        </Stack>
        <UserPanel />
      </Stack>
      <UserConfirmDialog />
    </Stack>
  )
}
