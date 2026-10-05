import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { isApiError } from '@shared/api/client'
import { useConfirmApproval, useRejectApproval } from '../api/approvalsApi.gen'
import { ApprovalsProvider } from '../ApprovalsProvider'
import { approvalsQueryOptions } from '../model/approvalsQuery'
import { ApprovalsPage } from './ApprovalsPage'

/** Owns the queue query and the confirm/reject mutations; refreshes the queue after every decision. */
export function ApprovalsContainer() {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const { data } = useSuspenseQuery(approvalsQueryOptions())
  const refresh = () => queryClient.invalidateQueries({ queryKey: approvalsQueryOptions().queryKey })
  const confirm = useConfirmApproval({ mutation: { onSettled: refresh } })
  const reject = useRejectApproval({ mutation: { onSettled: refresh } })
  const failed = confirm.error ?? reject.error

  function errorText(): string | null {
    if (!failed) return null
    if (isApiError(failed, 409, 404)) return t`That sign-off was already decided. The list has been refreshed.`
    if (isApiError(failed, 403)) return t`Only the reviewer named on a sign-off can decide it.`
    return t`The decision couldn't be saved. Please try again.`
  }

  const deciding = confirm.isPending
    ? (confirm.variables?.signatureId ?? null)
    : reject.isPending
      ? (reject.variables?.signatureId ?? null)
      : null

  return (
    <ApprovalsProvider
      groups={data.groups}
      deciding={deciding}
      error={errorText()}
      onConfirm={(signatureId) => {
        reject.reset()
        confirm.mutate({ signatureId })
      }}
      onReject={(signatureId, reason) => {
        confirm.reset()
        reject.mutate({ signatureId, data: { reason } })
      }}
    >
      <ApprovalsPage />
    </ApprovalsProvider>
  )
}
