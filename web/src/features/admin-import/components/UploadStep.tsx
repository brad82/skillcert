import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Autocomplete from '@mui/material/Autocomplete'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { Download, Upload } from 'lucide-react'
import { useState } from 'react'
import { templateCsv } from '../model/csvFile'
import { useUpload } from '../ImportProvider'

const uploadStepStyles = () => ({
  card: { p: 6, flex: '1 1 420px', minWidth: 0 },
  drop: { p: 8, border: 2, borderStyle: 'dashed', borderColor: 'lineStrong', borderRadius: 2, textAlign: 'center', bgcolor: 'background.default' },
  dragging: { borderColor: 'primary.main', bgcolor: 'brandTint' },
  example: { m: 0, p: 3, border: 1, borderColor: 'divider', borderRadius: 1, fontFamily: 'IBM Plex Mono, monospace', fontSize: 12, whiteSpace: 'pre-wrap', wordBreak: 'break-all' },
})

/** Choose (or drop) a CSV, optionally pick a list to add the new competencies to, with the format beside it. */
export function UploadStep() {
  const { problem, busy, choose, lists, targetListId, setTargetListId, headings, parentNodeId, setParentNodeId } = useUpload()
  const { t } = useLingui()
  const [dragging, setDragging] = useState(false)
  const styles = uploadStepStyles()
  const listOptions = [{ id: null, label: t`Don't add to a list` }, ...lists.map((l) => ({ id: l.id as string | null, label: l.title }))]
  const headingOptions = [{ id: null, label: t`Top level of the list` }, ...headings.map((h) => ({ id: h.id as string | null, label: [h.headingCode, h.headingTitle].filter(Boolean).join(' ') }))]

  return (
    <Stack direction="row" spacing={6} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'stretch' }}>
      <Paper variant="outlined" sx={styles.card}>
        <Stack spacing={4}>
          <Typography variant="h2"><Trans>Choose a CSV file</Trans></Typography>
          {problem === 'tooBig' && <Alert severity="error"><Trans>That file is over 1 MB. Split it into smaller files.</Trans></Alert>}
          {problem === 'notCsv' && <Alert severity="error"><Trans>Choose a .csv file.</Trans></Alert>}
          <Box
            sx={[styles.drop, dragging && styles.dragging]}
            onDragOver={(e) => {
              e.preventDefault()
              setDragging(true)
            }}
            onDragLeave={() => setDragging(false)}
            onDrop={(e) => {
              e.preventDefault()
              setDragging(false)
              const file = e.dataTransfer.files[0]
              if (file) void choose(file)
            }}
          >
            <Stack spacing={3} sx={{ alignItems: 'center' }}>
              <Upload size={40} aria-hidden />
              <Typography sx={{ fontWeight: 600 }}><Trans>Drop your file here</Trans></Typography>
              <Button variant="contained" component="label" disabled={busy}>
                <Trans>Choose file</Trans>
                <input
                  hidden
                  type="file"
                  accept=".csv,text/csv"
                  onChange={(e) => {
                    const file = e.target.files?.[0]
                    e.target.value = ''
                    if (file) void choose(file)
                  }}
                />
              </Button>
              <Typography variant="body2" color="text.secondary">
                <Trans>.csv, up to 1 MB. The file is checked straight away and nothing is imported until you confirm.</Trans>
              </Typography>
            </Stack>
          </Box>
          <Stack spacing={3} component="fieldset" sx={{ border: 0, m: 0, p: 0, pt: 4, borderTop: 1, borderColor: 'divider' }}>
            <Typography component="legend" variant="h3" sx={{ mb: 2 }}>
              <Trans>Add them to a list</Trans> <Typography component="span" variant="body2" color="text.secondary"><Trans>(optional)</Trans></Typography>
            </Typography>
            <Stack direction="row" spacing={3} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Autocomplete
                options={listOptions}
                value={listOptions.find((o) => o.id === targetListId) ?? listOptions[0]}
                onChange={(_, option) => setTargetListId(option?.id ?? null)}
                isOptionEqualToValue={(a, b) => a.id === b.id}
                disableClearable
                renderInput={(params) => <TextField {...params} label={t`List`} />}
                sx={{ flex: '1 1 220px' }}
              />
              <Autocomplete
                options={headingOptions}
                value={headingOptions.find((o) => o.id === parentNodeId) ?? headingOptions[0]}
                onChange={(_, option) => setParentNodeId(option?.id ?? null)}
                isOptionEqualToValue={(a, b) => a.id === b.id}
                disableClearable
                disabled={!targetListId}
                renderInput={(params) => <TextField {...params} label={t`Under heading`} />}
                sx={{ flex: '1 1 220px' }}
              />
            </Stack>
          </Stack>
        </Stack>
      </Paper>
      <Paper variant="outlined" sx={styles.card} component="section" aria-labelledby="format-heading">
        <Stack spacing={4}>
          <Typography id="format-heading" variant="h2"><Trans>File format</Trans></Typography>
          <Stack component="ul" spacing={2} sx={{ m: 0, pl: 5 }}>
            <li><Typography variant="body2"><Trans>Only Code and Title are required. Columns can be in any order.</Trans></Typography></li>
            <li><Typography variant="body2"><Trans>Review columns (SelfReview, PeerReview, InstructorReview, SupervisorReview) take Y or blank. The lowest Y counts; higher levels are implied.</Trans></Typography></li>
            <li><Typography variant="body2"><Trans>Resources: Title|URL|Type, separated by ;. Type is WebPage (the default), Video or Document.</Trans></Typography></li>
            <li><Typography variant="body2"><Trans>Leave RecertificationDays empty if the skill never expires.</Trans></Typography></li>
          </Stack>
          <Typography variant="overline" color="text.secondary"><Trans>Example</Trans></Typography>
          <Box component="pre" sx={styles.example}>{templateCsv}</Box>
          <Button
            variant="outlined"
            startIcon={<Download size={20} aria-hidden />}
            href={`data:text/csv;charset=utf-8,${encodeURIComponent(templateCsv)}`}
            download="competencies-template.csv"
            sx={{ alignSelf: 'flex-start' }}
          >
            <Trans>Download a template</Trans>
          </Button>
        </Stack>
      </Paper>
    </Stack>
  )
}
