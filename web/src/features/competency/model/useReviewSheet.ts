import { useMemo, useState } from 'react'
import type { MyHistoryEntryDto, MyReviewDto } from '@shared/api/model'

export type ReviewSheet = {
  review: MyReviewDto | null
  open: (reviewId: string) => void
  close: () => void
}

/**
 * Which history review is open in the bottom sheet. Owns only the selection; knows nothing about how the
 * review or its signature is drawn.
 */
export function useReviewSheet(history: MyHistoryEntryDto[]): ReviewSheet {
  const [reviewId, setReviewId] = useState<string | null>(null)
  const review = useMemo(
    () => history.find((entry) => entry.review?.id === reviewId)?.review ?? null,
    [history, reviewId],
  )
  return { review, open: setReviewId, close: () => setReviewId(null) }
}
