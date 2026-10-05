import type { ReviewLevelDto } from '@shared/api/model'
import type { BasketItem } from './basketItem'

/** Basket items that the same lowest reviewer level can sign; higher levels are implied (decision §2.5). */
export type BasketGroup = { level: ReviewLevelDto; items: BasketItem[] }

/** Groups by lowest permitted reviewer, lowest level first (Self → Peer → Instructor → Supervisor). */
export function groupByLowestReviewer(items: BasketItem[]): BasketGroup[] {
  const groups = new Map<number, BasketGroup>()
  for (const item of items) {
    const group = groups.get(item.lowestReviewer.order) ?? { level: item.lowestReviewer, items: [] }
    group.items.push(item)
    groups.set(item.lowestReviewer.order, group)
  }
  return [...groups.values()].sort((a, b) => a.level.order - b.level.order)
}

/**
 * One group holding everything, at the highest group's level: anyone who can sign that group can sign every
 * lower one too (permitted methods are closed upward). Null when there is nothing to merge.
 */
export function mergeAll(groups: BasketGroup[]): BasketGroup | null {
  if (groups.length < 2) return null
  return { level: groups[groups.length - 1]!.level, items: groups.flatMap((g) => g.items) }
}
