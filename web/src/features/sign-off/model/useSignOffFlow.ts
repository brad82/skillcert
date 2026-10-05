import { useMemo, useState } from 'react'
import type { ReviewOutcome, SignOffReviewerDto } from '@shared/api/model'

/** Hand-off (candidate holds the phone) → reviewer mode (find, assess, sign) → result (back to the candidate). */
export type SignOffStep = 'handoff' | 'findReviewer' | 'assess' | 'sign'

export type SignOffFlow = {
  step: SignOffStep
  reviewer: SignOffReviewerDto | null
  outcomes: Record<string, ReviewOutcome>
  comment: string
  query: string
  /** The candidate passes the phone over. */
  startReview: () => void
  setQuery: (query: string) => void
  chooseReviewer: (reviewer: SignOffReviewerDto) => void
  /** Flip one item between Competent and Not competent. */
  toggleOutcome: (competencyId: string) => void
  setComment: (comment: string) => void
  toSign: () => void
  back: () => void
  /** EXIT: hand the phone back, keeping nothing the reviewer entered. */
  exit: () => void
  counts: { competent: number; notCompetent: number }
}

/**
 * The sign-off wizard's state (wireframe 5a). Every item starts Competent; the reviewer flips the exceptions.
 * Knows nothing about the API call or the signature strokes.
 */
export function useSignOffFlow(competencyIds: string[]): SignOffFlow {
  const initialOutcomes = useMemo(
    () => Object.fromEntries(competencyIds.map((id) => [id, 'Competent' as ReviewOutcome])),
    [competencyIds],
  )
  const [step, setStep] = useState<SignOffStep>('handoff')
  const [reviewer, setReviewer] = useState<SignOffReviewerDto | null>(null)
  const [outcomes, setOutcomes] = useState<Record<string, ReviewOutcome>>(initialOutcomes)
  const [comment, setComment] = useState('')
  const [query, setQuery] = useState('')

  const exit = () => {
    setReviewer(null)
    setOutcomes(initialOutcomes)
    setComment('')
    setQuery('')
    setStep('handoff')
  }

  const notCompetent = Object.values(outcomes).filter((o) => o === 'NotCompetent').length

  return {
    step,
    reviewer,
    outcomes,
    comment,
    query,
    startReview: () => setStep('findReviewer'),
    setQuery,
    chooseReviewer: (chosen) => {
      setReviewer(chosen)
      setStep('assess')
    },
    toggleOutcome: (id) => setOutcomes((current) => ({ ...current, [id]: current[id] === 'NotCompetent' ? 'Competent' : 'NotCompetent' })),
    setComment,
    toSign: () => setStep('sign'),
    back: () => setStep((current) => (current === 'sign' ? 'assess' : 'findReviewer')),
    exit,
    counts: { competent: competencyIds.length - notCompetent, notCompetent },
  }
}
