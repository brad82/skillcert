import { useState } from 'react'

/** Which headings are collapsed in the tree. Everything starts expanded. */
export function useCollapsed() {
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(new Set())
  return {
    collapsed,
    toggle: (id: string) =>
      setCollapsed((current) => {
        const next = new Set(current)
        if (!next.delete(id)) next.add(id)
        return next
      }),
  }
}
