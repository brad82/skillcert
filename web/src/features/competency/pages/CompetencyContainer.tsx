import { useSuspenseQuery } from '@tanstack/react-query'
import { myCompetencyQueryOptions } from '@features/my-record'
import { CompetencyProvider } from '../CompetencyProvider'
import type { CompetencyTab } from '../model/competencyTabs'
import { CompetencyPage } from './CompetencyPage'

type Props = {
  competencyId: string
  tab: CompetencyTab
  onTabChange: (tab: CompetencyTab) => void
  onBack: () => void
}

/** Owns the competency query (the route loader has already fetched it, or thrown not-found). */
export function CompetencyContainer({ competencyId, tab, onTabChange, onBack }: Props) {
  const { data } = useSuspenseQuery(myCompetencyQueryOptions(competencyId))

  return (
    <CompetencyProvider competency={data} tab={tab} onTabChange={onTabChange} onBack={onBack}>
      <CompetencyPage />
    </CompetencyProvider>
  )
}
