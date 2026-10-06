import { Plural, Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import ToggleButton from '@mui/material/ToggleButton'
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup'
import Typography from '@mui/material/Typography'
import { Check, CircleX, TriangleAlert } from 'lucide-react'
import { signingAuthorityLabel } from '@shared/lib/reviewers'
import type { RowFilter } from '../model/useImportDraft'
import { usePreview } from '../ImportProvider'

/** Every row with its checks. Import stays off until every error is fixed: all rows go in together, or none. */
export function PreviewStep() {
  const preview = usePreview()
  const { counts } = preview
  const { t, i18n } = useLingui()
  return (
    <Stack spacing={4}>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <div>
          <Typography variant="h2"><Trans>Preview</Trans></Typography>
          <Typography variant="body2" color="text.secondary">
            {preview.fileName}
            {preview.target && <> → {preview.target}{preview.targetHeading && <> › {preview.targetHeading}</>}</>} ·{' '}
            <Trans>{counts.rows} rows checked · {counts.ready} ready · {counts.errors} with errors · {counts.warnings} with warnings</Trans>
          </Typography>
        </div>
        <ToggleButtonGroup exclusive size="small" value={preview.filter} onChange={(_, value: RowFilter | null) => value && preview.setFilter(value)} aria-label={t`Show rows`}>
          <ToggleButton value="all"><Trans>All {counts.rows}</Trans></ToggleButton>
          <ToggleButton value="errors"><Trans>Errors {counts.errors}</Trans></ToggleButton>
          <ToggleButton value="warnings"><Trans>Warnings {counts.warnings}</Trans></ToggleButton>
        </ToggleButtonGroup>
      </Stack>
      {preview.fileErrors.map((error) => <Alert key={error} severity="error">{error}</Alert>)}
      {!preview.canImport && (
        <Alert severity="error">
          <Plural value={counts.errors} one="Fix # error in your file, then go back and upload it again." other="Fix # errors in your file, then go back and upload it again." />{' '}
          <Trans>Nothing has been created. Warnings don't block the import.</Trans>
        </Alert>
      )}
      <TableContainer component={Paper} variant="outlined" sx={{ position: 'relative' }}>
        <Table aria-label={t`Rows`} size="small">
          <TableHead>
            <TableRow>
              <TableCell><Trans>Line</Trans></TableCell>
              <TableCell><Trans>Code</Trans></TableCell>
              <TableCell><Trans>Title</Trans></TableCell>
              <TableCell><Trans>Who can sign</Trans></TableCell>
              <TableCell><Trans>Recertification (days)</Trans></TableCell>
              <TableCell><Trans>Resources</Trans></TableCell>
              <TableCell sx={{ minWidth: 260 }}><Trans>Checks</Trans></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {preview.rows.map((row) => (
              <TableRow key={row.line} sx={row.errors.length > 0 ? { bgcolor: 'brandTint' } : undefined}>
                <TableCell><Typography variant="date">{row.line}</Typography></TableCell>
                <TableCell><Typography variant="code">{row.code}</Typography></TableCell>
                <TableCell>{row.title}</TableCell>
                <TableCell>{row.lowestReviewer ? i18n._(signingAuthorityLabel(row.lowestReviewer)) : '—'}</TableCell>
                <TableCell>{row.recertificationDays ?? <Trans>Never expires</Trans>}</TableCell>
                <TableCell>{row.resourceCount}</TableCell>
                <TableCell>
                  <Stack spacing={1}>
                    {row.errors.map((e) => (
                      <Stack key={e} direction="row" spacing={1} sx={{ color: 'error.main' }}><CircleX size={16} aria-hidden /><Typography variant="body2"><strong><Trans>Error:</Trans></strong> {e}</Typography></Stack>
                    ))}
                    {row.warnings.map((w) => (
                      <Stack key={w} direction="row" spacing={1} sx={{ color: 'text.secondary' }}><TriangleAlert size={16} aria-hidden /><Typography variant="body2"><strong><Trans>Warning:</Trans></strong> {w}</Typography></Stack>
                    ))}
                    {row.errors.length === 0 && row.warnings.length === 0 && (
                      <Stack direction="row" spacing={1}><Check size={16} aria-hidden /><Typography variant="body2"><Trans>Ready</Trans></Typography></Stack>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <Button variant="contained" disabled={!preview.canImport || preview.busy} onClick={preview.importAll}>
          <Plural value={counts.rows} one="Import # competency" other="Import # competencies" />
        </Button>
        <Button variant="outlined" onClick={preview.back}><Trans>Back to upload</Trans></Button>
        {!preview.canImport && <Typography variant="body2" color="text.secondary"><Trans>Import is available once every error is fixed.</Trans></Typography>}
      </Stack>
    </Stack>
  )
}
