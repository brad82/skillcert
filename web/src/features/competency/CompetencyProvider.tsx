import { createContext, type ReactNode, useContext, useMemo } from 'react'
import { toBasketItem, useBasket } from '@features/basket'
import type { MyCompetencyResponse } from '@shared/api/model'
import { basketActionState, groupResources, type BasketActionState, type ResourceGroup } from './model/competencyFacts'
import type { CompetencyTab } from './model/competencyTabs'
import { type ReviewSheet, useReviewSheet } from './model/useReviewSheet'

type Props = {
  competency: MyCompetencyResponse
  tab: CompetencyTab
  onTabChange: (tab: CompetencyTab) => void
  onBack: () => void
  children: ReactNode
}

type CompetencyContextValue = {
  competency: MyCompetencyResponse
  tab: CompetencyTab
  onTabChange: (tab: CompetencyTab) => void
  onBack: () => void
  resourceGroups: ResourceGroup[]
  sheet: ReviewSheet
}

const CompetencyContext = createContext<CompetencyContextValue | null>(null)

/** Composes the detail screen's tab, resource grouping and review-sheet models; no data-layer dependency. */
export function CompetencyProvider({ competency, tab, onTabChange, onBack, children }: Props) {
  const sheet = useReviewSheet(competency.history)
  const resourceGroups = useMemo(() => groupResources(competency.resources), [competency.resources])
  return (
    <CompetencyContext.Provider value={{ competency, tab, onTabChange, onBack, resourceGroups, sheet }}>
      {children}
    </CompetencyContext.Provider>
  )
}

function useCompetencyContext() {
  const context = useContext(CompetencyContext)
  if (!context) throw new Error('Competency hooks must be used within a CompetencyProvider')
  return context
}

/** App-bar row and the pinned status: code, short title, currency, back. */
export function useCompetencyHeader() {
  const { competency, onBack } = useCompetencyContext()
  return { code: competency.code, shortTitle: competency.shortTitle, currency: competency.currency, onBack }
}

export function useCompetencyTabs() {
  const { tab, onTabChange } = useCompetencyContext()
  return { tab, onTabChange }
}

/** Everything on the Overview tab (wireframe 3c). */
export function useOverview(): MyCompetencyResponse {
  return useCompetencyContext().competency
}

export function useResourceGroups(): ResourceGroup[] {
  return useCompetencyContext().resourceGroups
}

export function useHistory() {
  const { competency, sheet } = useCompetencyContext()
  return { entries: competency.history, openReview: sheet.open }
}

export function useReviewSheetState(): ReviewSheet {
  return useCompetencyContext().sheet
}

export type BasketAction = { state: BasketActionState; onClick: () => void }

/** The bottom basket button. */
export function useBasketAction(): BasketAction {
  const { competency } = useCompetencyContext()
  const basket = useBasket()
  const state = basketActionState(competency, basket.has(competency.competencyId))
  return { state, onClick: () => basket.toggle(toBasketItem(competency)) }
}
