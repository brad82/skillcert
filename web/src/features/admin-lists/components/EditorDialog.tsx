import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

type Props = {
  open: boolean
  title: ReactNode
  subtitle?: ReactNode
  error: string | null
  submitLabel: ReactNode
  canSubmit: boolean
  busy: boolean
  onClose: () => void
  onSubmit: () => void
  /** Left of the buttons, e.g. "2 selected". */
  footerNote?: ReactNode
  children: ReactNode
}

/** The frame every list-editor dialog shares: a form with title, error, fields, Cancel and the action. */
export function EditorDialog({ open, title, subtitle, error, submitLabel, canSubmit, busy, onClose, onSubmit, footerNote, children }: Props) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <form
        noValidate
        onSubmit={(event) => {
          event.preventDefault()
          onSubmit()
        }}
      >
        <DialogTitle>
          {title}
          {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={4} sx={{ pt: 2 }}>
            {error && <Alert severity="error">{error}</Alert>}
            {children}
          </Stack>
        </DialogContent>
        <DialogActions>
          {footerNote && <Typography variant="body2" color="text.secondary" sx={{ mr: 'auto', pl: 2 }}>{footerNote}</Typography>}
          <Button onClick={onClose}><Trans>Cancel</Trans></Button>
          <Button type="submit" variant="contained" disabled={!canSubmit || busy}>{submitLabel}</Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
