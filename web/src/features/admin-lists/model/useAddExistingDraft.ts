import { useState } from 'react'
import type { AdminCompetencyListItemDto } from '@shared/api/model'

export type AddExistingDraft = {
  parentId: string | null
  search: string
  selected: ReadonlySet<string>
  setParentId: (id: string | null) => void
  setSearch: (search: string) => void
  toggle: (competencyId: string) => void
  reset: (parentId: string | null, search?: string) => void
  /** Library matches for the search (code or title), capped so the dialog stays short. */
  results: (library: AdminCompetencyListItemDto[]) => AdminCompetencyListItemDto[]
}

const maxResults = 50

/** Picking existing competencies to share into this list. Knows nothing about the API. */
export function useAddExistingDraft(): AddExistingDraft {
  const [parentId, setParentId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set())
  const needle = search.trim().toLocaleLowerCase()
  return {
    parentId,
    search,
    selected,
    setParentId,
    setSearch,
    toggle: (id) =>
      setSelected((current) => {
        const next = new Set(current)
        if (!next.delete(id)) next.add(id)
        return next
      }),
    reset: (parent, s = '') => {
      setParentId(parent)
      setSearch(s)
      setSelected(new Set())
    },
    results: (library) =>
      library
        .filter((c) => needle === '' || c.code.toLocaleLowerCase().includes(needle) || c.title.toLocaleLowerCase().includes(needle))
        .slice(0, maxResults),
  }
}
