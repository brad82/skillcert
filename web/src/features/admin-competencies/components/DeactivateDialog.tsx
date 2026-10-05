import { Trans } from '@lingui/react/macro'
import { ConfirmDialog } from '@shared/components/ConfirmDialog'
import { useDeactivateConfirm } from '../CompetencyEditorProvider'

export function DeactivateDialog() {
  const { open, competency, busy, cancel, confirm } = useDeactivateConfirm()
  const code = competency.code
  const title = competency.current.title
  const lists = competency.lists.length
  return (
    <ConfirmDialog
      open={open}
      title={<Trans>Deactivate {code} {title}?</Trans>}
      stops={[<Trans key="1">Nobody can be signed off on {code}.</Trans>]}
      kept={[
        <Trans key="1">Every sign-off and review already made.</Trans>,
        <Trans key="2">Its place in {lists} lists.</Trans>,
        <Trans key="3">You can reactivate it at any time.</Trans>,
      ]}
      confirmLabel={<Trans>Deactivate {code}</Trans>}
      busy={busy}
      onCancel={cancel}
      onConfirm={() => void confirm()}
    />
  )
}
