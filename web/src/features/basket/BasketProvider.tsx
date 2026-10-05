import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { BasketItem } from './model/basketItem'

export type Basket = {
  items: BasketItem[]
  count: number
  has: (competencyId: string) => boolean
  add: (item: BasketItem) => void
  remove: (competencyId: string) => void
  toggle: (item: BasketItem) => void
  removeMany: (competencyIds: string[]) => void
  clear: () => void
}

const BasketContext = createContext<Basket | null>(null)

const storageKey = (userId: string) => `basket:${userId}`

function load(userId: string): BasketItem[] {
  try {
    const raw = sessionStorage.getItem(storageKey(userId))
    return raw ? (JSON.parse(raw) as BasketItem[]) : []
  } catch {
    return []
  }
}

/**
 * The sign-off basket (spec §9): UI-only, never a server entity. Kept per signed-in user for this browser
 * tab (sessionStorage), so a refresh mid-sign-off doesn't lose it. Knows nothing about grouping or
 * sign-off — see the basket feature's model.
 */
export function BasketProvider({ userId, children }: { userId: string; children: ReactNode }) {
  const [items, setItems] = useState<BasketItem[]>(() => load(userId))

  useEffect(() => {
    sessionStorage.setItem(storageKey(userId), JSON.stringify(items))
  }, [items, userId])

  const has = useCallback((competencyId: string) => items.some((i) => i.competencyId === competencyId), [items])
  const add = useCallback(
    (item: BasketItem) => setItems((current) => (current.some((i) => i.competencyId === item.competencyId) ? current : [...current, item])),
    [],
  )
  const remove = useCallback((competencyId: string) => setItems((current) => current.filter((i) => i.competencyId !== competencyId)), [])
  const removeMany = useCallback(
    (competencyIds: string[]) => setItems((current) => current.filter((i) => !competencyIds.includes(i.competencyId))),
    [],
  )
  const toggle = useCallback(
    (item: BasketItem) =>
      setItems((current) =>
        current.some((i) => i.competencyId === item.competencyId)
          ? current.filter((i) => i.competencyId !== item.competencyId)
          : [...current, item],
      ),
    [],
  )
  const clear = useCallback(() => setItems([]), [])

  const value = useMemo(
    () => ({ items, count: items.length, has, add, remove, toggle, removeMany, clear }),
    [items, has, add, remove, toggle, removeMany, clear],
  )
  return <BasketContext.Provider value={value}>{children}</BasketContext.Provider>
}

export function useBasket(): Basket {
  const context = useContext(BasketContext)
  if (!context) throw new Error('useBasket must be used within a BasketProvider')
  return context
}
