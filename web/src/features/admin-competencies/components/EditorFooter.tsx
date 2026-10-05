import { Plural, Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useEditorFooter } from '../CompetencyEditorProvider'

/** One way to save: "Publish changes…" opens the review, which decides edit or new revision. */
export function EditorFooter() {
  const { changeCount, canPublish, publish, discard } = useEditorFooter()
  return (
    <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', pt: 4, borderTop: 1, borderColor: 'divider' }}>
      <Button variant="contained" disabled={!canPublish} onClick={publish}><Trans>Publish changes…</Trans></Button>
      <Button variant="outlined" disabled={changeCount === 0} onClick={discard}><Trans>Discard</Trans></Button>
      <Typography variant="body2" color="text.secondary">
        {changeCount === 0 ? <Trans>No unsaved changes.</Trans> : <Plural value={changeCount} one="# unsaved change. You'll review it before anything is saved." other="# unsaved changes. You'll review them before anything is saved." />}
      </Typography>
    </Stack>
  )
}
