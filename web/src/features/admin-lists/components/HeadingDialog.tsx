import { Trans, useLingui } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useHeadingDialog } from '../ListEditorProvider'
import { EditorDialog } from './EditorDialog'
import { ParentPicker } from './ParentPicker'

/** Adds a heading (choosing where) or renames one. */
export function HeadingDialog() {
  const { open, renaming, draft, parents, busy, error, close, submit } = useHeadingDialog()
  const { t } = useLingui()
  return (
    <EditorDialog
      open={open}
      title={renaming ? <Trans>Rename heading</Trans> : <Trans>Add heading</Trans>}
      error={error}
      submitLabel={renaming ? <Trans>Save</Trans> : <Trans>Add heading</Trans>}
      canSubmit={draft.canSubmit}
      busy={busy}
      onClose={close}
      onSubmit={() => void submit()}
    >
      {!renaming && <ParentPicker label={t`Under`} value={draft.parentId} onChange={draft.setParentId} headings={parents} />}
      <Stack direction="row" spacing={3}>
        <TextField label={t`Code`} value={draft.code} onChange={(e) => draft.setCode(e.target.value)} sx={{ width: 120 }} helperText={t`e.g. 4.1`} />
        <TextField label={t`Title`} required autoFocus value={draft.title} onChange={(e) => draft.setTitle(e.target.value)} sx={{ flexGrow: 1 }} />
      </Stack>
    </EditorDialog>
  )
}
