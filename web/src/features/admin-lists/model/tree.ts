import type { AdminListNodeDto } from '@shared/api/model'

/** Pure helpers over a list's nodes, which the API sends in display order (depth-first). */

export const childrenOf = (nodes: AdminListNodeDto[], parentId: string | null) =>
  nodes.filter((node) => node.parentNodeId === parentId).sort((a, b) => a.index - b.index)

/** Every node under `id`, at any depth. */
export function descendantsOf(nodes: AdminListNodeDto[], id: string): AdminListNodeDto[] {
  const direct = childrenOf(nodes, id)
  return direct.flatMap((child) => [child, ...descendantsOf(nodes, child.id)])
}

export const competencyCountUnder = (nodes: AdminListNodeDto[], id: string) =>
  descendantsOf(nodes, id).filter((node) => node.kind === 'Competency').length

/** "4.3 Cardiopulmonary resuscitation (CPR)", or "4.3.1 One-rescuer adult CPR". */
export const nodeLabel = (node: AdminListNodeDto) =>
  node.kind === 'Heading'
    ? [node.headingCode, node.headingTitle].filter(Boolean).join(' ')
    : `${node.competency?.code ?? ''} ${node.competency?.title ?? ''}`.trim()

/** Nodes to draw: everything whose ancestors are all expanded. */
export function visibleNodes(nodes: AdminListNodeDto[], collapsed: ReadonlySet<string>): AdminListNodeDto[] {
  const hidden = new Set<string>()
  for (const id of collapsed) for (const node of descendantsOf(nodes, id)) hidden.add(node.id)
  return nodes.filter((node) => !hidden.has(node.id))
}

/** Headings a node may move under or be added under: not itself, nor anything inside it. */
export function headingTargets(nodes: AdminListNodeDto[], moving?: string): AdminListNodeDto[] {
  const excluded = moving ? new Set([moving, ...descendantsOf(nodes, moving).map((n) => n.id)]) : new Set<string>()
  return nodes.filter((node) => node.kind === 'Heading' && !excluded.has(node.id))
}

export const siblingCount = (nodes: AdminListNodeDto[], parentId: string | null) => childrenOf(nodes, parentId).length

export const competencyIdsIn = (nodes: AdminListNodeDto[]) =>
  new Set(nodes.flatMap((node) => (node.competency ? [node.competency.id] : [])))
