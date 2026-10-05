import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import { useNewList } from '../ListsProvider'
import { ListDetailsFields } from './ListDetailsFields'

export function NewListDialog() {
  const { open, draft, saving, error, cancel, submit } = useNewList()
  return (
    <Dialog open={open} onClose={cancel} fullWidth maxWidth="sm">
      <form onSubmit={(e) => { e.preventDefault(); void submit() }} noValidate>
        <DialogTitle><Trans>New list</Trans></DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <ListDetailsFields draft={draft} />
        </DialogContent>
        <DialogActions>
          <Button onClick={cancel}><Trans>Cancel</Trans></Button>
          <Button type="submit" variant="contained" disabled={!draft.canSubmit || saving}><Trans>Create list</Trans></Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
