import { Trans, useLingui } from '@lingui/react/macro'
import { ConfirmDialog } from '@shared/components/ConfirmDialog'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useUserConfirm } from '../UsersProvider'

/** Asks before deactivating a user or removing one of their classifications. */
export function UserConfirmDialog() {
  const { pending, user, busy, cancel, confirm } = useUserConfirm()
  const { i18n } = useLingui()
  const name = user?.displayName ?? ''
  const firstName = name.split(' ')[0]

  if (pending?.kind === 'removeClassification') {
    const classification = i18n._(reviewerLabel('Classified', pending.code))
    return (
      <ConfirmDialog
        open
        title={<Trans>Remove {classification} from {name}?</Trans>}
        stops={[<Trans key="1">Candidates can't send {firstName} new reviews that need {classification}.</Trans>]}
        kept={[
          <Trans key="1">{firstName} can still confirm or reject claims that already name them.</Trans>,
          <Trans key="1">Sign-offs {firstName} already gave still count.</Trans>,
          <Trans key="1">You can assign {classification} again at any time.</Trans>,
        ]}
        confirmLabel={<Trans>Remove {classification}</Trans>}
        busy={busy}
        onCancel={cancel}
        onConfirm={confirm}
      />
    )
  }

  return (
    <ConfirmDialog
      open={pending?.kind === 'deactivate'}
      title={<Trans>Deactivate {name}?</Trans>}
      stops={[<Trans key="1">{firstName} can't sign in.</Trans>, <Trans key="2">{firstName} can't be chosen as a reviewer.</Trans>]}
      kept={[<Trans key="1">Their record, sign-offs and every review they gave.</Trans>, <Trans key="2">You can reactivate {firstName} at any time.</Trans>]}
      confirmLabel={<Trans>Deactivate {firstName}</Trans>}
      busy={busy}
      onCancel={cancel}
      onConfirm={confirm}
    />
  )
}
