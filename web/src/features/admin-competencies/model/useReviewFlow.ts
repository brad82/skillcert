import { useState } from 'react'
import { type ChangedField, tabOf } from './useCompetencyDraft'

export type SaveAs = 'edit' | 'revision'
export type ReviewStep = 'review' | 'signoffs' | 'confirm'

export type ReviewFlow = {
  step: ReviewStep | null
  saveAs: SaveAs
  setSaveAs: (saveAs: SaveAs) => void
  /** Certification changes change who is current, so they can only be a new revision (API: competency.policy-change). */
  editAllowed: boolean
  recommended: SaveAs
  /** null until the admin chooses; there is no default. */
  invalidates: boolean | null
  setInvalidates: (invalidates: boolean) => void
  understood: boolean
  setUnderstood: (understood: boolean) => void
  start: () => void
  goTo: (step: ReviewStep) => void
  close: () => void
}

/**
 * The "Publish changes" review: which fields changed, edit or new revision, then (for a revision) what happens
 * to existing sign-offs and, for reassessment, a final confirmation.
 *
 * Recommendation rule (agreed in the wireframes): one field changed → save as an edit; two or more → a new
 * revision; anything on Certification → a new revision only. The admin always chooses.
 */
export function useReviewFlow(changes: ChangedField[]): ReviewFlow {
  const [step, setStep] = useState<ReviewStep | null>(null)
  const [saveAs, setSaveAs] = useState<SaveAs>('edit')
  const [invalidates, setInvalidates] = useState<boolean | null>(null)
  const [understood, setUnderstood] = useState(false)
  const editAllowed = !changes.some((field) => tabOf[field] === 'certification')
  const recommended: SaveAs = editAllowed && changes.length === 1 ? 'edit' : 'revision'
  return {
    step,
    saveAs: editAllowed ? saveAs : 'revision',
    setSaveAs,
    editAllowed,
    recommended,
    invalidates,
    setInvalidates,
    understood,
    setUnderstood,
    start: () => {
      setSaveAs(recommended)
      setInvalidates(null)
      setUnderstood(false)
      setStep('review')
    },
    goTo: setStep,
    close: () => setStep(null),
  }
}
