import { Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import IconButton from '@mui/material/IconButton'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { Plus, Trash2 } from 'lucide-react'
import { useDraft } from '../CompetencyEditorProvider'

/** Links for now (title + full address). Uploading videos and documents comes in a later release. */
export function ResourcesTab() {
  const draft = useDraft()
  const { t } = useLingui()
  return (
    <Stack spacing={6} role="tabpanel" id="panel-resources" aria-labelledby="tab-resources">
      <Stack spacing={3}>
        <div>
          <Typography variant="h3"><Trans>Links</Trans> <Typography component="span" variant="body2" color="text.secondary"><Trans>({draft.links.length} of 20)</Trans></Typography></Typography>
          <Typography variant="body2" color="text.secondary"><Trans>Each link needs a title and a full address starting with https://</Trans></Typography>
        </div>
        {draft.links.map((link, index) => {
          const n = index + 1
          const problem = draft.problems.links[index]
          return (
            <Stack key={index} direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'flex-start' } }}>
              <TextField label={t`Link ${n} title`} value={link.title} onChange={(e) => draft.setLink(index, { title: e.target.value })} error={problem === 'required'} sx={{ flex: 2 }} />
              <TextField
                label={t`Link ${n} address`}
                value={link.url}
                onChange={(e) => draft.setLink(index, { url: e.target.value })}
                error={problem === 'invalid'}
                helperText={problem === 'invalid' ? t`Use a full address starting with https://` : ' '}
                sx={{ flex: 3 }}
              />
              <IconButton aria-label={t`Remove link ${n}`} onClick={() => draft.removeLink(index)} sx={{ mt: { sm: 2 } }}>
                <Trash2 size={20} aria-hidden />
              </IconButton>
            </Stack>
          )
        })}
        <Button variant="outlined" startIcon={<Plus size={20} aria-hidden />} onClick={draft.addLink} disabled={draft.links.length >= 20} sx={{ alignSelf: 'flex-start' }}>
          <Trans>Add link</Trans>
        </Button>
      </Stack>
      <Paper variant="outlined" sx={{ p: 4, borderStyle: 'dashed', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 3, color: 'text.secondary' }}>
        <Typography variant="body2"><strong><Trans>Videos and documents</Trans></strong><br /><Trans>Uploading videos and documents comes in a later release.</Trans></Typography>
        <Chip size="small" variant="outlined" label={<Trans>Later</Trans>} />
      </Paper>
    </Stack>
  )
}
