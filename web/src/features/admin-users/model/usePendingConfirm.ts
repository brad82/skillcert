import { useState } from 'react'

export type PendingChange = { kind: 'deactivate'; userId: string } | { kind: 'removeClassification'; userId: string; code: string }

export type PendingConfirm = {
  pending: PendingChange | null
  ask: (change: PendingChange) => void
  cancel: () => void
}

/** The destructive change waiting on the confirmation dialog. Knows nothing about what confirming does. */
export function usePendingConfirm(): PendingConfirm {
  const [pending, setPending] = useState<PendingChange | null>(null)
  return { pending, ask: setPending, cancel: () => setPending(null) }
}
