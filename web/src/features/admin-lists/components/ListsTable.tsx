import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import { TextLink } from '@shared/components/RouterLinks'
import { useListsTable } from '../ListsProvider'

/** One row per list; the title opens the list editor. */
export function ListsTable() {
  const { rows } = useListsTable()
  const { t } = useLingui()
  return (
    <TableContainer component={Paper} variant="outlined" sx={{ position: 'relative' }}>
      <Table aria-label={t`Competency lists`}>
        <TableHead>
          <TableRow>
            <TableCell><Trans>List</Trans></TableCell>
            <TableCell><Trans>Competencies</Trans></TableCell>
            <TableCell><Trans>Assigned groups</Trans></TableCell>
            <TableCell><Trans>Status</Trans></TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((list) => (
            <TableRow key={list.id} sx={list.isActive ? undefined : { '& td': { color: 'text.secondary' } }}>
              <TableCell>
                <TextLink to="/admin/lists/$listId" params={{ listId: list.id }} color="inherit" sx={{ fontWeight: 600 }}>
                  {list.title}
                </TextLink>
                {list.description && <Typography variant="body2" color="text.secondary">{list.description}</Typography>}
              </TableCell>
              <TableCell>{list.competencyCount}</TableCell>
              <TableCell>{list.groups.length > 0 ? list.groups.join(', ') : <Trans>No groups</Trans>}</TableCell>
              <TableCell>{list.isActive ? <Trans>Active</Trans> : <Chip size="small" variant="outlined" label={<Trans>Inactive</Trans>} />}</TableCell>
            </TableRow>
          ))}
          {rows.length === 0 && (
            <TableRow><TableCell colSpan={4}><Typography color="text.secondary"><Trans>No lists match.</Trans></Typography></TableCell></TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
