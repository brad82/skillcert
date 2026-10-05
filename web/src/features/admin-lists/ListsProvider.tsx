import { createContext, type ReactNode, useContext, useState } from 'react'
import type { AdminListSummaryDto } from '@shared/api/model'
import { type ListDetailsDraft, useListDetailsDraft } from './model/useListDetailsDraft'

type Props = {
  lists: AdminListSummaryDto[]
  saving: boolean
  error: string | null
  /** Resolves true once the list exists (the container then opens it). */
  onCreate: (title: string, description: string | null) => Promise<boolean>
  children: ReactNode
}

type ListsContextValue = Omit<Props, 'children'> & {
  search: string
  setSearch: (search: string) => void
  creating: boolean
  setCreating: (open: boolean) => void
  draft: ListDetailsDraft
}

const ListsContext = createContext<ListsContextValue | null>(null)

/** The lists index: search over the loaded lists and the new-list dialog. No data-layer dependency. */
export function ListsProvider({ children, ...props }: Props) {
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const draft = useListDetailsDraft()
  return <ListsContext.Provider value={{ ...props, search, setSearch, creating, setCreating, draft }}>{children}</ListsContext.Provider>
}

function useListsContext() {
  const context = useContext(ListsContext)
  if (!context) throw new Error('Lists hooks must be used within a ListsProvider')
  return context
}

export function useListsTable() {
  const { lists, search, setSearch } = useListsContext()
  const needle = search.trim().toLocaleLowerCase()
  return { rows: lists.filter((list) => list.title.toLocaleLowerCase().includes(needle)), search, setSearch }
}

export function useNewList() {
  const { creating, setCreating, draft, saving, error, onCreate } = useListsContext()
  return {
    open: creating,
    draft,
    saving,
    error: creating ? error : null,
    start: () => {
      draft.reset()
      setCreating(true)
    },
    cancel: () => setCreating(false),
    submit: async () => {
      if (!draft.canSubmit) return
      if (await onCreate(draft.title.trim(), draft.description.trim() || null)) setCreating(false)
    },
  }
}
