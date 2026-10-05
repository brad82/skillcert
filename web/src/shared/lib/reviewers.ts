import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import type { ReviewLevelDto, ReviewMethod } from '@shared/api/model'

const methodLabels: Record<'Self' | 'Peer', MessageDescriptor> = { Self: msg`Self`, Peer: msg`Peer` }

const classificationLabels: Record<string, MessageDescriptor> = { Instructor: msg`Instructor`, Supervisor: msg`Supervisor` }

/**
 * Who gave, or may give, a review: "Self", "Peer", or the classification ("Instructor", "Supervisor").
 * An unknown classification code shows as-is.
 */
export function reviewerLabel(method: ReviewMethod, classificationCode: string | null): MessageDescriptor {
  if (method !== 'Classified') return methodLabels[method]
  const code = classificationCode ?? ''
  return classificationLabels[code] ?? { id: code, message: code }
}

export const reviewLevelLabel = (level: ReviewLevelDto) => reviewerLabel(level.method, level.classificationCode)
