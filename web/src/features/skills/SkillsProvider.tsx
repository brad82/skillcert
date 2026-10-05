import { createContext, type ReactNode, useContext, useMemo } from 'react'
import { buildListTree, type ListTreeNode } from '@features/my-record'
import type { MyListDto } from '@shared/api/model'
import type { PresentationState } from '@shared/lib/competencyStatus'
import { type Expansion, useExpansion } from './model/useExpansion'
import { type SkillsFilterState, useSkillsFilter } from './model/useSkillsFilter'
import { filterTree, isNarrowed, type VisibleNode } from './model/skillsTree'

type Props = {
  lists: MyListDto[]
  status: PresentationState | undefined
  onClearStatus: () => void
  onOpenCompetency: (competencyId: string) => void
  onOpenBasket: () => void
  children: ReactNode
}

export type ListView = { list: MyListDto; roots: ListTreeNode[] }

type SkillsContextValue = {
  views: ListView[]
  filterState: SkillsFilterState
  onOpenCompetency: (competencyId: string) => void
  onOpenBasket: () => void
}

const SkillsContext = createContext<SkillsContextValue | null>(null)

/** Composes the skills list's filter and tree models; no data-layer dependency. */
export function SkillsProvider({ lists, status, onClearStatus, onOpenCompetency, onOpenBasket, children }: Props) {
  const filterState = useSkillsFilter(status, onClearStatus)
  const views = useMemo(() => lists.map((list) => ({ list, roots: buildListTree(list) })), [lists])
  return <SkillsContext.Provider value={{ views, filterState, onOpenCompetency, onOpenBasket }}>{children}</SkillsContext.Provider>
}

function useSkillsContext() {
  const context = useContext(SkillsContext)
  if (!context) throw new Error('Skills hooks must be used within a SkillsProvider')
  return context
}

/** Search box and filter chips. */
export function useSkillsToolbar() {
  return useSkillsContext().filterState
}

/** Every required list with its tree pruned to the current filter. */
export function useSkillsLists(): ListView[] {
  return useSkillsContext().views
}

/** One list's visible tree and expansion; call once per rendered list section. */
export function useListSection(view: ListView): { visible: VisibleNode[]; expansion: Expansion } {
  const { filterState } = useSkillsContext()
  const expansion = useExpansion(view.roots, isNarrowed(filterState.filter))
  const visible = useMemo(() => filterTree(view.roots, filterState.filter), [view.roots, filterState.filter])
  return { visible, expansion }
}

export function useOpenCompetency() {
  return useSkillsContext().onOpenCompetency
}

export function useOpenBasket() {
  return useSkillsContext().onOpenBasket
}
