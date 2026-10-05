import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { useCreateList } from '../api/adminListsApi.gen'
import { ListsProvider } from '../ListsProvider'
import { listsQueryOptions } from '../model/listsQuery'
import { ListsPage } from './ListsPage'

/** Owns the lists query and the create mutation; opens a new list once it exists. */
export function ListsContainer() {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { data } = useSuspenseQuery(listsQueryOptions())
  const create = useCreateList()

  return (
    <ListsProvider
      lists={data.lists}
      saving={create.isPending}
      error={create.error ? t`The list couldn't be created. Please try again.` : null}
      onCreate={async (title, description) => {
        try {
          const list = await create.mutateAsync({ data: { title, description } })
          await queryClient.invalidateQueries({ queryKey: listsQueryOptions().queryKey })
          await navigate({ to: '/admin/lists/$listId', params: { listId: list.id } })
          return true
        } catch {
          return false
        }
      }}
    >
      <ListsPage />
    </ListsProvider>
  )
}
