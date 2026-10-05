import { useState } from 'react'

export type RejectDialog = {
  /** The sitting being rejected, while the dialog is open. */
  signatureId: string | null
  reason: string
  open: (signatureId: string) => void
  setReason: (reason: string) => void
  close: () => void
  canSubmit: boolean
}

/** The reject dialog's state: which sitting and the reason (required). Knows nothing about the API call. */
export function useRejectDialog(): RejectDialog {
  const [signatureId, setSignatureId] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  return {
    signatureId,
    reason,
    open: (id) => {
      setSignatureId(id)
      setReason('')
    },
    setReason,
    close: () => setSignatureId(null),
    canSubmit: reason.trim().length > 0,
  }
}
