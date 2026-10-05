import { createContext, type ReactNode, useContext } from 'react'
import type { ApprovalGroupDto } from '@shared/api/model'
import { type RejectDialog, useRejectDialog } from './model/useRejectDialog'

type Props = {
  groups: ApprovalGroupDto[]
  /** The sitting with a decision in flight. */
  deciding: string | null
  error: string | null
  onConfirm: (signatureId: string) => void
  onReject: (signatureId: string, reason: string) => void
  children: ReactNode
}

type ApprovalsContextValue = Omit<Props, 'children'> & { rejectDialog: RejectDialog }

const ApprovalsContext = createContext<ApprovalsContextValue | null>(null)

/** Composes the approval queue's reject dialog with the decisions passed in; no data-layer dependency. */
export function ApprovalsProvider({ children, ...props }: Props) {
  const rejectDialog = useRejectDialog()
  return <ApprovalsContext.Provider value={{ ...props, rejectDialog }}>{children}</ApprovalsContext.Provider>
}

function useApprovalsContext() {
  const context = useContext(ApprovalsContext)
  if (!context) throw new Error('Approvals hooks must be used within an ApprovalsProvider')
  return context
}

export function useApprovalGroups() {
  const { groups, error } = useApprovalsContext()
  return { groups, error }
}

/** Confirm and reject for one sitting card. */
export function useApprovalActions(signatureId: string) {
  const { deciding, onConfirm, rejectDialog } = useApprovalsContext()
  return {
    busy: deciding === signatureId,
    disabled: deciding !== null,
    confirm: () => onConfirm(signatureId),
    reject: () => rejectDialog.open(signatureId),
  }
}

export function useRejectDialogState() {
  const { rejectDialog, onReject, groups } = useApprovalsContext()
  const group = groups.find((g) => g.signatureId === rejectDialog.signatureId) ?? null
  return {
    ...rejectDialog,
    group,
    submit: () => {
      if (!rejectDialog.signatureId || !rejectDialog.canSubmit) return
      onReject(rejectDialog.signatureId, rejectDialog.reason.trim())
      rejectDialog.close()
    },
  }
}
