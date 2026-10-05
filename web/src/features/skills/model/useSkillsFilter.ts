import { useState } from 'react'
import type { PresentationState } from '@shared/lib/competencyStatus'
import type { QuickFilter, SkillsFilter } from './skillsTree'

export type SkillsFilterState = {
  filter: SkillsFilter
  setText: (text: string) => void
  setQuick: (quick: QuickFilter) => void
  clearStatus: () => void
}

/**
 * Owns the search text and quick filter. The single-state filter comes from the URL (a Home tile) and is
 * cleared through `onClearStatus`, so the address bar stays the truth for it. Knows nothing about expansion.
 */
export function useSkillsFilter(status: PresentationState | undefined, onClearStatus: () => void): SkillsFilterState {
  const [text, setText] = useState('')
  const [quick, setQuick] = useState<QuickFilter>('all')
  return { filter: { text, quick, status }, setText, setQuick, clearStatus: onClearStatus }
}
