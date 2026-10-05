import { useState } from 'react'
import type { AdminCompetencyListItemDto } from '@shared/api/model'

export type CompetencyShow = 'all' | 'unlisted' | 'shared' | 'inactive'

/**
 * All competencies' search (code or title) and filter. Filters the loaded library in the browser;
 * knows nothing about the API.
 */
export function useCompetencyFilters() {
  const [search, setSearch] = useState('')
  const [show, setShow] = useState<CompetencyShow>('all')
  const needle = search.trim().toLocaleLowerCase()
  const matchesShow = (c: AdminCompetencyListItemDto) =>
    show === 'all' || (show === 'unlisted' && c.listCount === 0) || (show === 'shared' && c.listCount > 1) || (show === 'inactive' && !c.isActive)
  return {
    search,
    setSearch,
    show,
    setShow,
    apply: (items: AdminCompetencyListItemDto[]) =>
      items.filter((c) => matchesShow(c) && (needle === '' || c.code.toLocaleLowerCase().includes(needle) || c.title.toLocaleLowerCase().includes(needle))),
  }
}

export type CompetencyFilters = ReturnType<typeof useCompetencyFilters>
