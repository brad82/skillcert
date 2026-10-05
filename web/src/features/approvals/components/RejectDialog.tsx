import { Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useRejectDialogState } from '../ApprovalsProvider'

/** Reject a whole sitting with a reason the candidate will see. Rejection is final. */
export function RejectDialog() {
  const { signatureId, group, reason, setReason, close, canSubmit, submit } = useRejectDialogState()
  const { t } = useLingui()
  const candidate = group?.candidateName ?? ''

  return (
    <Dialog open={signatureId !== null} onClose={close} fullWidth>
      <DialogTitle>
        <Trans>Reject {candidate}'s sign-off?</Trans>
      </DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          <Trans>None of these skills will count, and this can't be undone. The candidate sees your reason.</Trans>
        </Typography>
        <TextField
          label={t`Reason`}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          required
          multiline
          minRows={2}
          fullWidth
          autoFocus
          slotProps={{ htmlInput: { maxLength: 500 } }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>
          <Trans>Cancel</Trans>
        </Button>
        <Button color="error" variant="contained" onClick={submit} disabled={!canSubmit}>
          <Trans>Reject</Trans>
        </Button>
      </DialogActions>
    </Dialog>
  )
}
