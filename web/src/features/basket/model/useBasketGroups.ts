import { useMemo, useState } from 'react'
import { type BasketGroup, groupByLowestReviewer, mergeAll } from './basketGroups'
import type { BasketItem } from './basketItem'

export type BasketGroups = {
  groups: BasketGroup[]
  /** The single group a merge would produce, while a merge is possible and not yet applied. */
  mergeSuggestion: BasketGroup | null
  merge: () => void
}

/**
 * The basket screen's grouping (wireframe 4a) and its one-shot merge. Owns only presentation: removing items
 * and the sign-off itself belong to the basket store and the sign-off flow.
 */
export function useBasketGroups(items: BasketItem[]): BasketGroups {
  const [merged, setMerged] = useState(false)
  const separate = useMemo(() => groupByLowestReviewer(items), [items])
  const all = useMemo(() => mergeAll(separate), [separate])
  return {
    groups: merged && all ? [all] : separate,
    mergeSuggestion: merged ? null : all,
    merge: () => setMerged(true),
  }
}
