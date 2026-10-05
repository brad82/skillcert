import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import type { ConfirmationStatus, ReviewOutcome } from '@shared/api/model'

export const outcomeLabels: Record<ReviewOutcome, MessageDescriptor> = {
  Competent: msg`Competent`,
  NotCompetent: msg`Not competent`,
}

/** Only shown when a reviewer confirmation applies; NotRequired has no label. */
export const confirmationLabels: Record<ConfirmationStatus, MessageDescriptor | null> = {
  NotRequired: null,
  Pending: msg`Waiting for confirmation`,
  Confirmed: msg`Confirmed`,
  Rejected: msg`Rejected`,
}
