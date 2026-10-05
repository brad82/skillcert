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

/** The four lowest-signer choices (development plan §2.5); every higher level is implied. */
export type SigningAuthority = 'Self' | 'Peer' | 'Instructor' | 'Supervisor'

export const signingAuthorities: { key: SigningAuthority; level: { method: ReviewMethod; classificationCode: string | null }; label: MessageDescriptor }[] = [
  { key: 'Self', level: { method: 'Self', classificationCode: null }, label: msg`Anyone (self or higher)` },
  { key: 'Peer', level: { method: 'Peer', classificationCode: null }, label: msg`Peer or higher` },
  { key: 'Instructor', level: { method: 'Classified', classificationCode: 'Instructor' }, label: msg`Instructor or higher` },
  { key: 'Supervisor', level: { method: 'Classified', classificationCode: 'Supervisor' }, label: msg`Supervisor only` },
]

/** Which of the four choices a stored level is. */
export const signingAuthorityOf = (level: { method: ReviewMethod; classificationCode: string | null }): SigningAuthority =>
  level.method === 'Classified' ? (level.classificationCode === 'Supervisor' ? 'Supervisor' : 'Instructor') : level.method

export const signingAuthorityLabel = (level: { method: ReviewMethod; classificationCode: string | null }) =>
  signingAuthorities.find((a) => a.key === signingAuthorityOf(level))!.label
