import { Trans, useLingui } from '@lingui/react/macro'
import Typography from '@mui/material/Typography'
import { nodeLabel } from '../model/tree'
import { useMoveDialog } from '../ListEditorProvider'
import { EditorDialog } from './EditorDialog'
import { ParentPicker } from './ParentPicker'

/** Moves a node under another heading (at the end), or to the top level. A heading can't go inside itself. */
export function MoveDialog() {
  const { open, node, target, setTarget, parents, busy, error, close, submit } = useMoveDialog()
  const { t } = useLingui()
  const label = node ? nodeLabel(node) : ''
  return (
    <EditorDialog open={open} title={<Trans>Move {label}</Trans>} error={error} submitLabel={<Trans>Move</Trans>} canSubmit busy={busy} onClose={close} onSubmit={() => void submit()}>
      <ParentPicker label={t`Move under`} value={target} onChange={setTarget} headings={parents} />
      <Typography variant="body2" color="text.secondary"><Trans>It goes at the end. Use Move up and Move down to fine-tune its place.</Trans></Typography>
    </EditorDialog>
  )
}
