import { Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Collapse from '@mui/material/Collapse'
import IconButton from '@mui/material/IconButton'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { Fragment } from 'react'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { actionLabels, entityTypeLabels } from '../model/actionLabels'
import { changeRows } from '../model/changeRows'
import { useAuditEntries } from '../AuditProvider'

const dateTimes = new Map<string, Intl.DateTimeFormat>()
const formatDateTime = (iso: string, locale: string) => {
  let format = dateTimes.get(locale)
  if (!format) dateTimes.set(locale, (format = new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' })))
  return format.format(new Date(iso))
}

/** Newest first. Each entry expands to its before/after values, field by field. */
export function AuditTable() {
  const { entries, expanded, toggle, hasMore, loadingMore, loadMore } = useAuditEntries()
  const { locale } = useLocale()
  const { t, i18n } = useLingui()
  return (
    <Stack spacing={4}>
      <TableContainer component={Paper} variant="outlined">
        <Table aria-label={t`Audit log`}>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox" />
              <TableCell><Trans>When</Trans></TableCell>
              <TableCell><Trans>Who</Trans></TableCell>
              <TableCell><Trans>What</Trans></TableCell>
              <TableCell><Trans>Item</Trans></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {entries.map((entry) => {
              const open = expanded.has(entry.id)
              const action = actionLabels[entry.action]
              const type = entityTypeLabels[entry.entityType]
              return (
                <Fragment key={entry.id}>
                  <TableRow sx={open ? { bgcolor: 'brandTint', '& td': { borderBottom: 0 } } : undefined}>
                    <TableCell padding="checkbox">
                      <IconButton aria-expanded={open} aria-label={open ? t`Hide details` : t`Show details`} onClick={() => toggle(entry.id)}>
                        {open ? <ChevronDown size={20} aria-hidden /> : <ChevronRight size={20} aria-hidden />}
                      </IconButton>
                    </TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}><Typography variant="date">{formatDateTime(entry.at, locale)}</Typography></TableCell>
                    <TableCell>{entry.actorName}</TableCell>
                    <TableCell>
                      <Typography sx={{ fontWeight: 600 }}>{action ? i18n._(action) : entry.action}</Typography>
                      <Typography variant="code" color="text.secondary">{entry.action}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="text.secondary" component="span">{type ? i18n._(type) : entry.entityType}</Typography>{' '}
                      {entry.entityLabel}
                    </TableCell>
                  </TableRow>
                  <TableRow sx={open ? { bgcolor: 'brandTint' } : undefined}>
                    <TableCell colSpan={5} sx={{ py: 0, ...(open ? {} : { borderBottom: 0 }) }}>
                      <Collapse in={open} unmountOnExit>
                        <Table size="small" aria-label={t`Changes`} sx={{ my: 3, bgcolor: 'background.paper', maxWidth: 720 }}>
                          <TableHead>
                            <TableRow>
                              <TableCell><Trans>Field</Trans></TableCell>
                              <TableCell><Trans>Before</Trans></TableCell>
                              <TableCell><Trans>After</Trans></TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {changeRows(entry.before, entry.after).map((row) => (
                              <TableRow key={row.field}>
                                <TableCell><Typography variant="code">{row.field}</Typography></TableCell>
                                <TableCell><Typography variant="code" color="text.secondary" sx={row.changed ? { textDecoration: 'line-through' } : undefined}>{row.before}</Typography></TableCell>
                                <TableCell><Typography variant="code" sx={row.changed ? { fontWeight: 600 } : undefined}>{row.after}</Typography></TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </Collapse>
                    </TableCell>
                  </TableRow>
                </Fragment>
              )
            })}
            {entries.length === 0 && (
              <TableRow><TableCell colSpan={5}><Typography color="text.secondary"><Trans>No changes match these filters.</Trans></Typography></TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      {hasMore && (
        <Button variant="outlined" disabled={loadingMore} onClick={loadMore} sx={{ alignSelf: 'flex-start' }}>
          <Trans>Load older entries</Trans>
        </Button>
      )}
    </Stack>
  )
}
