import type { ListTreeNode } from '@features/my-record'
import type { MyCompetencySummaryDto } from '@shared/api/model'
import { needsAction, type PresentationState, presentationState } from '@shared/lib/competencyStatus'

export type QuickFilter = 'all' | 'needsAction' | 'current'

export type SkillsFilter = {
  text: string
  quick: QuickFilter
  /** A single state chosen from a Home tile; cleared by the user. */
  status?: PresentationState
}

/** A tree node after filtering: headings keep only children with a match below them. */
export type VisibleNode = {
  source: ListTreeNode
  children: VisibleNode[]
}

export function matchesFilter(competency: MyCompetencySummaryDto, filter: SkillsFilter): boolean {
  const state = presentationState(competency.currency)
  if (filter.status && state !== filter.status) return false
  if (filter.quick === 'needsAction' && !needsAction(state)) return false
  if (filter.quick === 'current' && state !== 'current' && state !== 'expiringSoon') return false

  const text = filter.text.trim().toLocaleLowerCase()
  if (!text) return true
  return [competency.code, competency.title, competency.shortTitle].some((value) => value.toLocaleLowerCase().includes(text))
}

/** Prunes the tree to matching competencies and the headings above them. Heading progress stays unfiltered. */
export function filterTree(roots: ListTreeNode[], filter: SkillsFilter): VisibleNode[] {
  const visit = (treeNode: ListTreeNode): VisibleNode | null => {
    const competency = treeNode.node.competency
    if (competency) return matchesFilter(competency, filter) ? { source: treeNode, children: [] } : null
    const children = treeNode.children.map(visit).filter((child): child is VisibleNode => child !== null)
    return children.length > 0 ? { source: treeNode, children } : null
  }
  return roots.map(visit).filter((node): node is VisibleNode => node !== null)
}

export const isNarrowed = (filter: SkillsFilter) => filter.text.trim() !== '' || filter.quick !== 'all' || filter.status !== undefined
