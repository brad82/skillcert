import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Link from '@mui/material/Link'
import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import { visuallyHidden } from '@mui/utils'
import { formatDate } from '@shared/lib/dates'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { useUserRows } from '../UsersProvider'
import { ClassificationChips } from './ClassificationChips'

const usersTableStyles = () => ({
  selected: { bgcolor: 'brandTint' },
  inactive: { '& td': { color: 'text.secondary' } },
  name: { fontWeight: 600, textAlign: 'left' },
})

/** Every user matching the filters. Choosing a name opens the side panel; rows carry deactivate/reactivate. */
export function UsersTable() {
  const { rows, selectedId, select, currentUserId, busy, deactivate, reactivate } = useUserRows()
  const { locale } = useLocale()
  const { t } = useLingui()
  const styles = usersTableStyles()

  return (
    <TableContainer component={Paper} variant="outlined">
      <Table aria-label={t`Users`}>
        <TableHead>
          <TableRow>
            <TableCell><Trans>Name</Trans></TableCell>
            <TableCell><Trans>Reviewer classifications</Trans></TableCell>
            <TableCell><Trans>Groups</Trans></TableCell>
            <TableCell><Trans>Created</Trans></TableCell>
            <TableCell><Trans>Status</Trans></TableCell>
            <TableCell><Box component="span" sx={visuallyHidden}><Trans>Actions</Trans></Box></TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((user) => (
            <TableRow key={user.id} sx={[user.id === selectedId && styles.selected, !user.isActive && styles.inactive]}>
              <TableCell>
                <Link component="button" color="inherit" onClick={() => select(user.id)} sx={styles.name}>
                  {user.displayName}
                </Link>
                <Typography variant="body2" color="text.secondary">{user.email}</Typography>
              </TableCell>
              <TableCell><ClassificationChips codes={user.classifications} /></TableCell>
              <TableCell>{user.groups.length > 0 ? user.groups.join(', ') : <Trans>None</Trans>}</TableCell>
              <TableCell><Typography variant="date">{formatDate(user.createdAt, locale)}</Typography></TableCell>
              <TableCell>
                {user.isActive ? <Trans>Active</Trans> : <Chip size="small" variant="outlined" label={<Trans>Deactivated</Trans>} />}
              </TableCell>
              <TableCell align="right">
                {user.id === currentUserId ? (
                  <Typography variant="body2" color="text.secondary"><Trans>This is you</Trans></Typography>
                ) : user.isActive ? (
                  <Button size="small" variant="outlined" disabled={busy} onClick={() => deactivate(user.id)}><Trans>Deactivate</Trans></Button>
                ) : (
                  <Button size="small" variant="outlined" disabled={busy} onClick={() => reactivate(user.id)}><Trans>Reactivate</Trans></Button>
                )}
              </TableCell>
            </TableRow>
          ))}
          {rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={6}><Typography color="text.secondary"><Trans>No users match.</Trans></Typography></TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
