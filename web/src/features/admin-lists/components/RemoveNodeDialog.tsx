import { Trans } from '@lingui/react/macro'
import { ConfirmDialog } from '@shared/components/ConfirmDialog'
import { nodeLabel } from '../model/tree'
import { useRemoveDialog } from '../ListEditorProvider'

/** Asks before removing a heading (and everything under it) or a competency from the list. */
export function RemoveNodeDialog() {
  const { open, node, listTitle, competencyCount, busy, close, confirm } = useRemoveDialog()
  const label = node ? nodeLabel(node) : ''
  const code = node?.kind === 'Heading' ? (node.headingCode ?? node.headingTitle ?? '') : (node?.competency?.code ?? '')
  const heading = node?.kind === 'Heading'
  return (
    <ConfirmDialog
      open={open}
      title={heading ? <Trans>Remove {code} and everything under it?</Trans> : <Trans>Remove {label} from {listTitle}?</Trans>}
      stops={[
        heading ? (
          <Trans key="1">The heading {label} and its {competencyCount} competencies leave {listTitle} now.</Trans>
        ) : (
          <Trans key="1">{label} leaves {listTitle} now.</Trans>
        ),
        <Trans key="2">Patrollers on this list stop seeing them straight away.</Trans>,
      ]}
      kept={[<Trans key="1">All review history, and the competencies themselves.</Trans>, <Trans key="2">Archived PDFs keep what they showed.</Trans>]}
      confirmLabel={heading ? <Trans>Remove {code} and {competencyCount} competencies</Trans> : <Trans>Remove {code}</Trans>}
      busy={busy}
      onCancel={close}
      onConfirm={() => void confirm()}
    />
  )
}
