import { useCallback, useState } from 'react'
import type { ListTreeNode } from '@features/my-record'

export type Expansion = {
  isExpanded: (headingId: string) => boolean
  toggle: (headingId: string) => void
}

/**
 * Which headings are open. Defaults (wireframe 2a): a heading whose subtree is all current starts collapsed,
 * any other starts open. While the list is narrowed by search or filter, every heading shows open so matches
 * are never hidden. Knows nothing about filtering itself.
 */
export function useExpansion(roots: ListTreeNode[], narrowed: boolean): Expansion {
  const [overrides, setOverrides] = useState<Record<string, boolean>>({})

  const defaults = useDefaults(roots)
  const isExpanded = useCallback(
    (headingId: string) => (narrowed ? true : (overrides[headingId] ?? defaults.get(headingId) ?? true)),
    [narrowed, overrides, defaults],
  )
  const toggle = useCallback(
    (headingId: string) => setOverrides((current) => ({ ...current, [headingId]: !(current[headingId] ?? defaults.get(headingId) ?? true) })),
    [defaults],
  )
  return { isExpanded, toggle }
}

function useDefaults(roots: ListTreeNode[]): Map<string, boolean> {
  const [defaults] = useState(() => {
    const map = new Map<string, boolean>()
    const visit = (node: ListTreeNode) => {
      if (node.node.competency) return
      map.set(node.node.id, node.current < node.total)
      node.children.forEach(visit)
    }
    roots.forEach(visit)
    return map
  })
  return defaults
}
