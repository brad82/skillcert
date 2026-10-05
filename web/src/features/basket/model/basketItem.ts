import type { MyCompetencySummaryDto, ReviewLevelDto } from '@shared/api/model'

/** What the basket remembers about a competency. The server re-resolves everything at sign-off. */
export type BasketItem = {
  competencyId: string
  code: string
  title: string
  lowestReviewer: ReviewLevelDto
}

export const toBasketItem = (competency: MyCompetencySummaryDto): BasketItem => ({
  competencyId: competency.competencyId,
  code: competency.code,
  title: competency.shortTitle,
  lowestReviewer: competency.lowestReviewer,
})
