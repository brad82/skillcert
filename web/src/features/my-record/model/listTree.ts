import type { MyCompetencySummaryDto, MyListDto, MyListNodeDto } from '@shared/api/model'
import { type PresentationState, presentationState } from '@shared/lib/competencyStatus'

export type ListTreeNode = {
  node: MyListNodeDto
  children: ListTreeNode[]
  /** Competencies in this subtree, and how many are Current (expiring soon counts as current). */
  total: number
  current: number
}

/** Rebuilds the list's tree from the API's depth-first node list, with per-heading progress. */
export function buildListTree(list: MyListDto): ListTreeNode[] {
  const byId = new Map<string, ListTreeNode>()
  const roots: ListTreeNode[] = []
  for (const node of list.nodes) {
    const treeNode: ListTreeNode = { node, children: [], total: 0, current: 0 }
    byId.set(node.id, treeNode)
    const parent = node.parentNodeId ? byId.get(node.parentNodeId) : undefined
    ;(parent ? parent.children : roots).push(treeNode)
  }

  const tally = (treeNode: ListTreeNode): void => {
    if (treeNode.node.competency) {
      treeNode.total = 1
      treeNode.current = treeNode.node.competency.currency.status === 'Current' ? 1 : 0
      return
    }
    treeNode.children.forEach(tally)
    treeNode.total = treeNode.children.reduce((sum, child) => sum + child.total, 0)
    treeNode.current = treeNode.children.reduce((sum, child) => sum + child.current, 0)
  }
  roots.forEach(tally)
  return roots
}

/** Each competency once across all the candidate's lists (a shared competency is one requirement, spec §6). */
export function distinctCompetencies(lists: MyListDto[]): MyCompetencySummaryDto[] {
  const seen = new Map<string, MyCompetencySummaryDto>()
  for (const list of lists) {
    for (const node of list.nodes) {
      if (node.competency && !seen.has(node.competency.competencyId)) seen.set(node.competency.competencyId, node.competency)
    }
  }
  return [...seen.values()]
}

export type StateCounts = Record<PresentationState, number>

export function countStates(competencies: MyCompetencySummaryDto[]): StateCounts {
  const counts: StateCounts = { current: 0, expiringSoon: 0, expired: 0, notCompetent: 0, pending: 0, notCertified: 0 }
  for (const competency of competencies) counts[presentationState(competency.currency)]++
  return counts
}
