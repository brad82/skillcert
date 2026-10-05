import { Trans } from '@lingui/react/macro'
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
  /** Names the action and the thing: "Deactivate Ines Instructor?" */
  title: ReactNode
  /** What stops when confirmed. */
  stops: ReactNode[]
  /** What is kept. */
  kept: ReactNode[]
  /** Repeats the verb: "Deactivate Ines". */
  confirmLabel: ReactNode
  busy?: boolean
  onCancel: () => void
  onConfirm: () => void
}

const confirmDialogStyles = () => ({
  list: { m: 0, pl: 5 },
})

/**
 * The admin rule for anything destructive: a dialog that names the action, says what stops and what is
 * kept, and repeats the verb on the confirm button. Cancel has focus, so Enter does nothing harmful.
 */
export function ConfirmDialog({ open, title, stops, kept, confirmLabel, busy = false, onCancel, onConfirm }: Props) {
  const styles = confirmDialogStyles()
  const section = (heading: ReactNode, items: ReactNode[]) =>
    items.length > 0 && (
      <Stack spacing={1}>
        <Typography variant="overline" color="text.secondary">
          {heading}
        </Typography>
        <Stack component="ul" spacing={1} sx={styles.list}>
          {items.map((item, index) => (
            <li key={index}>
              <Typography>{item}</Typography>
            </li>
          ))}
        </Stack>
      </Stack>
    )

  return (
    <Dialog open={open} onClose={onCancel} fullWidth maxWidth="sm" role="alertdialog">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={4}>
          {section(<Trans>What stops</Trans>, stops)}
          {section(<Trans>What is kept</Trans>, kept)}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button autoFocus variant="outlined" onClick={onCancel}>
          <Trans>Cancel</Trans>
        </Button>
        <Button variant="contained" onClick={onConfirm} disabled={busy}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
