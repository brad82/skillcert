import type { CurrencyDto } from '@shared/api/model'

/**
 * What a candidate screen shows for a competency (spec §13). Derived from currency, never stored:
 * "expiring soon" is Current with expiry inside 30 days, and "pending" replaces a non-current status while
 * a claim waits for its reviewer. A Current skill with a pending reassessment still shows Current.
 */
export type PresentationState = 'current' | 'expiringSoon' | 'expired' | 'notCompetent' | 'pending' | 'notCertified'

export function presentationState(currency: CurrencyDto): PresentationState {
  if (currency.status === 'Current') return currency.expiringSoon ? 'expiringSoon' : 'current'
  if (currency.hasPendingReview) return 'pending'
  switch (currency.status) {
    case 'Expired':
      return 'expired'
    case 'NotCompetent':
      return 'notCompetent'
    default:
      return 'notCertified'
  }
}

/** Anything a candidate should act on: everything except a plain Current. */
export const needsAction = (state: PresentationState) => state !== 'current'

/** A pending claim can't be added to the basket again until the reviewer decides (wireframe 2a). */
export const canAddToBasket = (currency: CurrencyDto) => !currency.hasPendingReview
