import type { MyCompetencyResponse, MyResourceDto, ResourceType } from '@shared/api/model'
import { canAddToBasket } from '@shared/lib/competencyStatus'

/** "Every 1 year" for whole years of 365 days, otherwise days. Display only: expiry is always elapsed days. */
export type RecertificationPeriod = { unit: 'year' | 'day'; count: number } | null

export function recertificationPeriod(days: number | null): RecertificationPeriod {
  if (days === null) return null
  return days % 365 === 0 ? { unit: 'year', count: days / 365 } : { unit: 'day', count: days }
}

const resourceOrder: ResourceType[] = ['Video', 'WebPage', 'Document']

export type ResourceGroup = { type: ResourceType; resources: MyResourceDto[] }

/** Resources grouped Video, Web page, Document (wireframe 3d); empty groups are dropped. */
export function groupResources(resources: MyResourceDto[]): ResourceGroup[] {
  return resourceOrder
    .map((type) => ({ type, resources: resources.filter((r) => r.type === type) }))
    .filter((group) => group.resources.length > 0)
}

/**
 * The bottom action's state (wireframe 3b): add, add for reassessment when already current, remove when in
 * the basket, and disabled while a claim waits for its reviewer.
 */
export type BasketActionState = 'add' | 'reassess' | 'remove' | 'waiting'

export function basketActionState(competency: MyCompetencyResponse, inBasket: boolean): BasketActionState {
  if (inBasket) return 'remove'
  if (!canAddToBasket(competency.currency)) return 'waiting'
  return competency.currency.status === 'Current' ? 'reassess' : 'add'
}
