import { createContext, type ReactNode, useContext } from 'react'
import { useBasket } from './BasketProvider'
import type { BasketGroup } from './model/basketGroups'
import { type BasketGroups, useBasketGroups } from './model/useBasketGroups'

type Props = {
  onSignOff: (competencyIds: string[]) => void
  onOpenSkills: () => void
  children: ReactNode
}

type BasketPageContextValue = BasketGroups & { onSignOff: (group: BasketGroup) => void; onOpenSkills: () => void }

const BasketPageContext = createContext<BasketPageContextValue | null>(null)

/** Composes the basket store with the screen's grouping; sign-off and navigation are callbacks from the route. */
export function BasketPageProvider({ onSignOff, onOpenSkills, children }: Props) {
  const { items } = useBasket()
  const grouping = useBasketGroups(items)
  const value = { ...grouping, onSignOff: (group: BasketGroup) => onSignOff(group.items.map((i) => i.competencyId)), onOpenSkills }
  return <BasketPageContext.Provider value={value}>{children}</BasketPageContext.Provider>
}

function useBasketPageContext() {
  const context = useContext(BasketPageContext)
  if (!context) throw new Error('Basket page hooks must be used within a BasketPageProvider')
  return context
}

/** Title count and CLEAR. */
export function useBasketHeader() {
  const { count, clear } = useBasket()
  return { count, clear }
}

export function useBasketGroupList() {
  const { groups, onSignOff, onOpenSkills } = useBasketPageContext()
  const { remove } = useBasket()
  return { groups, onSignOff, remove, onOpenSkills }
}

export function useMergeSuggestion() {
  const { mergeSuggestion, merge } = useBasketPageContext()
  return { suggestion: mergeSuggestion, merge }
}
