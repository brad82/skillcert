import { Trans } from '@lingui/react/macro'
import { useRenameListDialog } from '../ListEditorProvider'
import { EditorDialog } from './EditorDialog'
import { ListDetailsFields } from './ListDetailsFields'

export function RenameListDialog() {
  const { open, draft, busy, error, close, submit } = useRenameListDialog()
  return (
    <EditorDialog open={open} title={<Trans>Rename list</Trans>} error={error} submitLabel={<Trans>Save</Trans>} canSubmit={draft.canSubmit} busy={busy} onClose={close} onSubmit={() => void submit()}>
      <ListDetailsFields draft={draft} />
    </EditorDialog>
  )
}
