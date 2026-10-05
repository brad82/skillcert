import { useState } from 'react'
import type { CreateCompetencyRequest } from '@shared/api/model'
import { type SigningAuthority, signingAuthorities } from '@shared/lib/reviewers'

export type FieldProblem = 'required' | 'invalid' | null

export type NewCompetencyDraft = {
  parentId: string | null
  code: string
  title: string
  shortTitle: string
  recertificationDays: string
  authority: SigningAuthority
  setParentId: (id: string | null) => void
  setCode: (code: string) => void
  setTitle: (title: string) => void
  setShortTitle: (shortTitle: string) => void
  setRecertificationDays: (days: string) => void
  setAuthority: (authority: SigningAuthority) => void
  reset: (parentId: string | null) => void
  problems: { code: FieldProblem; title: FieldProblem; recertificationDays: FieldProblem }
  showErrors: boolean
  /** The code the API refused as a duplicate, until the draft changes. */
  duplicateOf: string | null
  markDuplicate: () => void
  /** Marks a submit attempt; returns the request when the draft is valid. */
  submit: () => CreateCompetencyRequest | null
}

/**
 * A new competency created straight into a list: code, title, short title, recertification period and
 * signing authority. Description and resources are added in the editor afterwards. Mirrors the API's limits
 * (title ≤ 300, days 1–36500 or empty).
 */
export function useNewCompetencyDraft(): NewCompetencyDraft {
  const [parentId, setParentId] = useState<string | null>(null)
  const [code, setCode] = useState('')
  const [title, setTitle] = useState('')
  const [shortTitle, setShortTitle] = useState('')
  const [recertificationDays, setRecertificationDays] = useState('')
  const [authority, setAuthority] = useState<SigningAuthority>('Instructor')
  const [showErrors, setShowErrors] = useState(false)
  const [duplicateOf, setDuplicateOf] = useState<string | null>(null)

  const days = recertificationDays.trim()
  const daysValue = days === '' ? null : Number(days)
  const problems = {
    code: code.trim() === '' ? ('required' as const) : null,
    title: title.trim() === '' ? ('required' as const) : null,
    recertificationDays: daysValue !== null && !(Number.isInteger(daysValue) && daysValue >= 1 && daysValue <= 36500) ? ('invalid' as const) : null,
  }

  return {
    parentId,
    code,
    title,
    shortTitle,
    recertificationDays,
    authority,
    setParentId,
    setCode: (value) => {
      setCode(value)
      setDuplicateOf(null)
    },
    setTitle,
    setShortTitle,
    setRecertificationDays,
    setAuthority,
    reset: (parent) => {
      setParentId(parent)
      setCode('')
      setTitle('')
      setShortTitle('')
      setRecertificationDays('')
      setAuthority('Instructor')
      setShowErrors(false)
      setDuplicateOf(null)
    },
    problems,
    showErrors,
    duplicateOf,
    markDuplicate: () => setDuplicateOf(code.trim()),
    submit: () => {
      setShowErrors(true)
      if (problems.code || problems.title || problems.recertificationDays) return null
      return {
        code: code.trim(),
        content: {
          title: title.trim(),
          shortTitle: shortTitle.trim() || null,
          description: null,
          recertificationDays: daysValue,
          lowestReviewer: signingAuthorities.find((a) => a.key === authority)!.level,
          resources: [],
        },
      }
    },
  }
}
