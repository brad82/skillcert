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
import { signingAuthorityLabel } from '@shared/lib/reviewers'
import { useCompetencyRows } from '../CompetenciesProvider'
import { Recertification } from './Recertification'

export function CompetenciesTable() {
  const { rows } = useCompetencyRows()
  const { i18n, t } = useLingui()
  return (
    <TableContainer component={Paper} variant="outlined">
      <Table aria-label={t`All competencies`}>
        <TableHead>
          <TableRow>
            <TableCell><Trans>Code</Trans></TableCell>
            <TableCell><Trans>Title</Trans></TableCell>
            <TableCell><Trans>Who can sign</Trans></TableCell>
            <TableCell><Trans>Recertification</Trans></TableCell>
            <TableCell><Trans>Revision</Trans></TableCell>
            <TableCell><Trans>In lists</Trans></TableCell>
            <TableCell><Trans>Status</Trans></TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((c) => (
            <TableRow key={c.id} sx={c.isActive ? undefined : { '& td': { color: 'text.secondary' } }}>
              <TableCell><Typography variant="code">{c.code}</Typography></TableCell>
              <TableCell>
                <TextLink to="/admin/competencies/$competencyId" params={{ competencyId: c.id }} color="inherit">{c.title}</TextLink>
                {c.shortTitle !== c.title && <Typography variant="body2" color="text.secondary"><Trans>Short: {c.shortTitle}</Trans></Typography>}
              </TableCell>
              <TableCell>{i18n._(signingAuthorityLabel(c.lowestReviewer))}</TableCell>
              <TableCell><Recertification days={c.recertificationDays} /></TableCell>
              <TableCell><Typography variant="date">{c.revisionNumber}</Typography></TableCell>
              <TableCell>{c.listCount === 0 ? <Trans>Not in any list</Trans> : c.listCount}</TableCell>
              <TableCell>{c.isActive ? <Trans>Active</Trans> : <Chip size="small" variant="outlined" label={<Trans>Inactive</Trans>} />}</TableCell>
            </TableRow>
          ))}
          {rows.length === 0 && (
            <TableRow><TableCell colSpan={7}><Typography color="text.secondary"><Trans>No competencies match.</Trans></Typography></TableCell></TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
