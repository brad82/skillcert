import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { competenciesQueryOptions, useCreateCompetency } from '@features/admin-competencies'
import { problemType } from '@shared/api/client'
import type { AdminListDto } from '@shared/api/model'
import { useAddCompetencies, useAddHeading, useMoveNode, useRemoveNode, useRenameHeading, useRenameList } from '../api/adminListsApi.gen'
import { ListEditorProvider } from '../ListEditorProvider'
import { listQueryOptions, listsQueryOptions } from '../model/listsQuery'
import { ListEditorPage } from './ListEditorPage'

/**
 * Owns the list and library queries and every tree mutation. Each mutation returns the whole updated list,
 * which replaces the cached one; the lists index and the library (list counts) are refreshed behind it.
 */
export function ListEditorContainer({ listId }: { listId: string }) {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const { data: list } = useSuspenseQuery(listQueryOptions(listId))
  const { data: library } = useSuspenseQuery(competenciesQueryOptions())
  const [failure, setFailure] = useState<unknown>(null)
  const renameList = useRenameList()
  const addHeading = useAddHeading()
  const renameHeading = useRenameHeading()
  const moveNode = useMoveNode()
  const removeNode = useRemoveNode()
  const addCompetencies = useAddCompetencies()
  const createCompetency = useCreateCompetency()
  const busy = [renameList, addHeading, renameHeading, moveNode, removeNode, addCompetencies, createCompetency].some((m) => m.isPending)

  async function save(change: () => Promise<AdminListDto>): Promise<boolean> {
    try {
      queryClient.setQueryData(listQueryOptions(listId).queryKey, await change())
      setFailure(null)
      void queryClient.invalidateQueries({ queryKey: listsQueryOptions().queryKey })
      void queryClient.invalidateQueries({ queryKey: competenciesQueryOptions().queryKey })
      return true
    } catch (error) {
      setFailure(error)
      return false
    }
  }

  function errorText(): string | null {
    if (!failure) return null
    switch (problemType(failure)) {
      case 'list.duplicate-competency':
        return t`A competency can appear only once in a list, and one of those is already here.`
      case 'list.cycle':
        return t`A heading can't move inside itself. It stayed where it was.`
      case 'list.invalid-parent':
        return t`Only headings can hold items. Choose a heading.`
      default:
        return t`The change couldn't be saved. Please try again.`
    }
  }

  return (
    <ListEditorProvider
      list={list}
      library={library.competencies}
      busy={busy}
      error={errorText()}
      onRenameList={(title, description) => save(() => renameList.mutateAsync({ listId, data: { title, description } }))}
      onAddHeading={(parentNodeId, code, title) => save(() => addHeading.mutateAsync({ listId, data: { parentNodeId, code, title, index: null } }))}
      onRenameHeading={(nodeId, code, title) => save(() => renameHeading.mutateAsync({ listId, nodeId, data: { code, title } }))}
      onMove={(nodeId, parentNodeId, index) => save(() => moveNode.mutateAsync({ listId, nodeId, data: { parentNodeId, index } }))}
      onRemove={(nodeId) => save(() => removeNode.mutateAsync({ listId, nodeId }))}
      onAddCompetencies={(parentNodeId, competencyIds) =>
        save(() => addCompetencies.mutateAsync({ listId, data: { parentNodeId, competencyIds, index: null } }))
      }
      onCreateCompetency={async (parentNodeId, request) => {
        let created
        try {
          created = await createCompetency.mutateAsync({ data: request })
        } catch (error) {
          if (problemType(error) === 'competency.duplicate-code') return 'duplicate'
          setFailure(error)
          return 'failed'
        }
        // The competency now exists; adding it to the list is a second call.
        const added = await save(() => addCompetencies.mutateAsync({ listId, data: { parentNodeId, competencyIds: [created.id], index: null } }))
        return added ? 'ok' : 'failed'
      }}
    >
      <ListEditorPage />
    </ListEditorProvider>
  )
}
