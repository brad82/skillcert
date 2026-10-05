import { createContext, type ReactNode, useContext, useMemo } from 'react'
import type { BasketItem } from '@features/basket'
import type { ReviewLevelDto, SignOffRequest, SignOffResponse, SignOffReviewerDto } from '@shared/api/model'
import { type SignaturePad, useSignaturePad } from './model/useSignaturePad'
import { type SignOffFlow, useSignOffFlow } from './model/useSignOffFlow'

type Props = {
  items: BasketItem[]
  candidateName: string
  reviewers: SignOffReviewerDto[]
  submitting: boolean
  error: string | null
  result: SignOffResponse | null
  onSubmit: (request: SignOffRequest) => void
  onCancel: () => void
  onDone: () => void
  children: ReactNode
}

type SignOffContextValue = Omit<Props, 'children'> & { flow: SignOffFlow; pad: SignaturePad; level: ReviewLevelDto | null }

const SignOffContext = createContext<SignOffContextValue | null>(null)

/** Composes the sign-off wizard and signature pad models; the API call and navigation are callbacks. */
export function SignOffProvider({ children, ...props }: Props) {
  const ids = useMemo(() => props.items.map((i) => i.competencyId), [props.items])
  const flow = useSignOffFlow(ids)
  const pad = useSignaturePad()
  // The level every reviewer offered meets: the highest of the items' lowest levels.
  const level = props.items.reduce<ReviewLevelDto | null>((highest, i) => (!highest || i.lowestReviewer.order > highest.order ? i.lowestReviewer : highest), null)
  return <SignOffContext.Provider value={{ ...props, flow, pad, level }}>{children}</SignOffContext.Provider>
}

function useSignOffContext() {
  const context = useContext(SignOffContext)
  if (!context) throw new Error('Sign-off hooks must be used within a SignOffProvider')
  return context
}

export type SignOffScreen = 'empty' | 'handoff' | 'findReviewer' | 'assess' | 'sign' | 'result'

/** Which screen shows, and whether the reviewer holds the phone (black bar, EXIT). */
export function useSignOffChrome() {
  const { flow, result, items, onCancel } = useSignOffContext()
  const screen: SignOffScreen = result ? 'result' : items.length === 0 ? 'empty' : flow.step
  const reviewerMode = screen === 'findReviewer' || screen === 'assess' || screen === 'sign'
  return { screen, reviewerMode, exit: flow.exit, onCancel }
}

export function useHandoff() {
  const { candidateName, items, level, flow, onCancel } = useSignOffContext()
  return { candidateName, count: items.length, level, startReview: flow.startReview, onCancel }
}

/** "Find your name": registered users who can sign the whole group, filtered by the search. */
export function useReviewerSearch() {
  const { reviewers, flow } = useSignOffContext()
  const needle = flow.query.trim().toLocaleLowerCase()
  const matches = needle ? reviewers.filter((r) => r.displayName.toLocaleLowerCase().includes(needle)) : reviewers
  return { query: flow.query, setQuery: flow.setQuery, reviewers: matches, choose: flow.chooseReviewer }
}

export function useAssessment() {
  const { candidateName, items, flow } = useSignOffContext()
  return {
    candidateName,
    items,
    outcomes: flow.outcomes,
    toggle: flow.toggleOutcome,
    comment: flow.comment,
    setComment: flow.setComment,
    next: flow.toSign,
    back: flow.back,
  }
}

export function useSignStep() {
  const { candidateName, items, flow, pad, submitting, error, onSubmit } = useSignOffContext()
  const reviewer = flow.reviewer!
  const signatureRequired = reviewer.signatureRequired
  return {
    candidateName,
    reviewer,
    counts: flow.counts,
    pad,
    signatureRequired,
    canSubmit: !submitting && (!signatureRequired || pad.hasInk),
    submitting,
    error,
    back: flow.back,
    /** Pad size in CSS pixels, so the server can render the strokes at the right scale. */
    submit: (padWidth: number, padHeight: number) =>
      onSubmit({
        reviewerUserId: reviewer.userId,
        items: items.map((i) => ({ competencyId: i.competencyId, outcome: flow.outcomes[i.competencyId] ?? 'Competent' })),
        comment: flow.comment.trim() || null,
        signature: pad.hasInk ? pad.toRequest(padWidth, padHeight) : null,
      }),
  }
}

export function useSignOffResult() {
  const { result, onDone } = useSignOffContext()
  return { result: result!, onDone }
}

export function useEmptySignOff() {
  return { onDone: useSignOffContext().onDone }
}
