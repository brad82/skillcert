import { createContext, type ReactNode, useContext } from 'react'
import type { AdminCompetencyListItemDto } from '@shared/api/model'
import { type CompetencyFilters, useCompetencyFilters } from './model/useCompetencyFilters'

type Props = { competencies: AdminCompetencyListItemDto[]; children: ReactNode }

const CompetenciesContext = createContext<(CompetencyFilters & { competencies: AdminCompetencyListItemDto[] }) | null>(null)

/** All competencies: the library plus its filters. Read-only; editing starts from a row. */
export function CompetenciesProvider({ competencies, children }: Props) {
  const filters = useCompetencyFilters()
  return <CompetenciesContext.Provider value={{ ...filters, competencies }}>{children}</CompetenciesContext.Provider>
}

function useCompetenciesContext() {
  const context = useContext(CompetenciesContext)
  if (!context) throw new Error('Competencies hooks must be used within a CompetenciesProvider')
  return context
}

export function useCompetencyFilterFields() {
  const { search, setSearch, show, setShow } = useCompetenciesContext()
  return { search, setSearch, show, setShow }
}

export function useCompetencyRows() {
  const { competencies, apply } = useCompetenciesContext()
  return { rows: apply(competencies) }
}
