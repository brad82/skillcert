import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import FormControlLabel from '@mui/material/FormControlLabel'
import List from '@mui/material/List'
import ListItem from '@mui/material/ListItem'
import ListItemText from '@mui/material/ListItemText'
import RadioGroup from '@mui/material/RadioGroup'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Pencil } from 'lucide-react'
import { fieldLabels, tabLabels } from '../model/changeLabels'
import { tabOf } from '../model/useCompetencyDraft'
import type { SaveAs } from '../model/useReviewFlow'
import { useReview } from '../CompetencyEditorProvider'
import { ChoiceCard } from './ChoiceCard'

/**
 * "Publish changes" review, in up to three steps:
 * 1. which fields changed (no detail, so an accidental change stands out) and edit vs new revision;
 * 2. for a new revision, what happens to people already signed off (no default);
 * 3. for reassessment, a final confirmation that everyone's currency stops counting.
 */
export function ReviewDialog() {
  const review = useReview()
  const { i18n, t } = useLingui()
  const { competency, changes, step, busy, error } = review
  const code = competency.code
  const shared = competency.lists.length > 1
  const lists = competency.lists.map((l) => l.title).join(', ')

  return (
    <Dialog open={step !== null} onClose={review.close} fullWidth maxWidth="md" role={step === 'confirm' ? 'alertdialog' : 'dialog'}>
      {step === 'review' && (
        <>
          <DialogTitle><Trans>Review changes to {code} {competency.current.title}</Trans></DialogTitle>
          <DialogContent>
            <Stack spacing={5}>
              {error && <Alert severity="error">{error}</Alert>}
              <div>
                <Typography sx={{ fontWeight: 600 }}>{changes.length === 1 ? <Trans>You changed 1 item</Trans> : <Trans>You changed {changes.length} items</Trans>}</Typography>
                <Typography variant="body2" color="text.secondary"><Trans>Check these are the changes you meant to make.</Trans></Typography>
              </div>
              <List aria-label={t`Changed items`} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, py: 0 }}>
                {changes.map((field) => (
                  <ListItem key={field} divider secondaryAction={<Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: 'text.secondary' }}><Pencil size={16} aria-hidden /><Typography variant="body2"><Trans>Changed</Trans></Typography></Stack>}>
                    <ListItemText primary={<>{i18n._(tabLabels[tabOf[field]])} › <strong>{i18n._(fieldLabels[field])}</strong></>} />
                  </ListItem>
                ))}
              </List>
              {shared && <Alert severity="info"><Trans>{code} is shared by {competency.lists.length} lists: {lists}. Saving changes it in all of them.</Trans></Alert>}
              <Stack spacing={2}>
                <Typography sx={{ fontWeight: 600 }} id="save-as-label"><Trans>How should this be saved?</Trans></Typography>
                <RadioGroup aria-labelledby="save-as-label" value={review.saveAs} onChange={(e) => review.setSaveAs(e.target.value as SaveAs)} sx={{ flexDirection: { sm: 'row' }, gap: 3 }}>
                  <ChoiceCard
                    value="edit"
                    selected={review.saveAs === 'edit'}
                    disabled={!review.editAllowed}
                    badge={review.recommended === 'edit' ? <Trans>Recommended</Trans> : undefined}
                    title={<Trans>Save as an edit</Trans>}
                    description={
                      review.editAllowed ? (
                        <Trans>Updates revision {competency.current.number} in place. Nobody's sign-offs change.</Trans>
                      ) : (
                        <Trans>Not available: recertification and signing authority can only change in a new revision.</Trans>
                      )
                    }
                  />
                  <ChoiceCard
                    value="revision"
                    selected={review.saveAs === 'revision'}
                    badge={review.recommended === 'revision' ? <Trans>Recommended</Trans> : undefined}
                    title={<Trans>Publish a new revision</Trans>}
                    description={<Trans>Use when the skill itself has changed. You'll choose what happens to existing sign-offs next.</Trans>}
                  />
                </RadioGroup>
              </Stack>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={review.close}><Trans>Keep editing</Trans></Button>
            <Button variant="contained" disabled={busy} onClick={() => void review.next()}>
              {review.saveAs === 'edit' ? <Trans>Save edit</Trans> : <Trans>Continue</Trans>}
            </Button>
          </DialogActions>
        </>
      )}

      {step === 'signoffs' && (
        <>
          <DialogTitle><Trans>Publish a new revision of {code}</Trans></DialogTitle>
          <DialogContent>
            <Stack spacing={5}>
              {error && <Alert severity="error">{error}</Alert>}
              <Alert severity="info">
                <Trans>You are creating a new revision of this skill.</Trans>
                {shared && <> <Trans>It is shared by {competency.lists.length} lists ({lists}), so your choice applies to candidates on all of them.</Trans></>}
              </Alert>
              <Stack spacing={2}>
                <Typography sx={{ fontWeight: 600 }} id="signoffs-label"><Trans>What happens to people already signed off?</Trans></Typography>
                <Typography variant="body2" color="text.secondary"><Trans>Required. There is no default.</Trans></Typography>
                <RadioGroup aria-labelledby="signoffs-label" value={review.invalidates === null ? '' : String(review.invalidates)} onChange={(e) => review.setInvalidates(e.target.value === 'true')} sx={{ gap: 3 }}>
                  <ChoiceCard
                    value="false"
                    selected={review.invalidates === false}
                    title={<Trans>Review at their next recertification</Trans>}
                    description={<Trans>Existing sign-offs stay current until they expire. Each person is then assessed against the newest revision. Use this for clarifications and improvements that don't need anyone retrained straight away.</Trans>}
                  />
                  <ChoiceCard
                    value="true"
                    selected={review.invalidates === true}
                    title={<Trans>Require reassessment now</Trans>}
                    description={<Trans>All candidates will be required to demonstrate competency immediately. Use this for time-sensitive or critical policy updates.</Trans>}
                  />
                </RadioGroup>
              </Stack>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => review.goTo('review')}><Trans>Back</Trans></Button>
            <Button variant="contained" disabled={review.invalidates === null || busy} onClick={review.publishOrConfirm}><Trans>Publish new revision</Trans></Button>
          </DialogActions>
        </>
      )}

      {step === 'confirm' && (
        <>
          <DialogTitle><Trans>Publish a new revision and require reassessment?</Trans></DialogTitle>
          <DialogContent>
            <Stack spacing={4}>
              {error && <Alert severity="error">{error}</Alert>}
              <Typography><Typography component="span" variant="code">{code}</Typography> {competency.current.title}</Typography>
              <Stack component="ul" spacing={2} sx={{ m: 0, pl: 5 }}>
                <li><Typography><strong><Trans>Every candidate's sign-off on {code} stops counting the moment you publish.</Trans></strong> <Trans>They all show Not certified until signed off again.</Trans></Typography></li>
                {shared && <li><Typography><Trans>This applies on every list that uses {code}: {lists}.</Trans></Typography></li>}
                <li><Typography><Trans>Their old sign-offs stay in their history.</Trans></Typography></li>
              </Stack>
              <FormControlLabel
                control={<Checkbox checked={review.understood} onChange={(e) => review.setUnderstood(e.target.checked)} />}
                label={<Trans>I understand everyone signed off on {code} will need to be reassessed.</Trans>}
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button autoFocus onClick={() => review.goTo('signoffs')}><Trans>Go back</Trans></Button>
            <Button variant="contained" disabled={!review.understood || busy} onClick={review.publishWithReassessment}><Trans>Publish and require reassessment</Trans></Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  )
}
