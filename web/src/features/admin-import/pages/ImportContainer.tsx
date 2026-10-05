import { useLingui } from '@lingui/react/macro'
import { useQuery, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { competenciesQueryOptions } from '@features/admin-competencies'
import { listQueryOptions, listsQueryOptions, useAddCompetencies } from '@features/admin-lists'
import { isApiError, problemType } from '@shared/api/client'
import type { CompetencyImportPreviewResponse, CompetencyImportResponse } from '@shared/api/model'
import { useImportCompetencies, usePreviewCompetencyImport } from '../api/adminImportApi.gen'
import { ImportProvider } from '../ImportProvider'
import { ImportPage } from './ImportPage'

const isPreview = (body: unknown): body is CompetencyImportPreviewResponse => typeof body === 'object' && body !== null && 'rows' in body && 'canImport' in body

/**
 * Owns the preview and import mutations, and the optional target list. When a list is chosen, the import is
 * followed by a second call that adds the created competencies to it (their ids looked up by code).
 * A 422 carries a fresh preview, which replaces the shown one; nothing was created.
 */
export function ImportContainer() {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const { data: lists } = useSuspenseQuery(listsQueryOptions())
  const [targetListId, setTargetListId] = useState<string | null>(null)
  const target = useQuery({ ...listQueryOptions(targetListId ?? ''), enabled: targetListId !== null })
  const previewMutation = usePreviewCompetencyImport()
  const importMutation = useImportCompetencies()
  const addCompetencies = useAddCompetencies()
  const [preview, setPreview] = useState<CompetencyImportPreviewResponse | null>(null)
  const [result, setResult] = useState<CompetencyImportResponse | null>(null)
  const [failure, setFailure] = useState<'preview' | 'conflict' | 'import' | 'addToList' | null>(null)

  function errorText(): string | null {
    switch (failure) {
      case 'preview':
        return t`The file couldn't be checked. Please try again.`
      case 'conflict':
        return t`Someone created one of these codes while you were previewing. Go back and upload the file again.`
      case 'import':
        return t`The import failed and nothing was created. Please try again.`
      case 'addToList':
        return t`The competencies were created, but couldn't be added to the list. Add them from the list editor.`
      default:
        return null
    }
  }

  async function importAll(csv: string, parentNodeId: string | null) {
    setFailure(null)
    let imported: CompetencyImportResponse
    try {
      imported = await importMutation.mutateAsync({ data: { csv } })
    } catch (error) {
      if (isApiError(error, 422) && isPreview(error.body)) return setPreview(error.body)
      return setFailure(problemType(error) === 'import.conflict' ? 'conflict' : 'import')
    }
    await queryClient.invalidateQueries({ queryKey: competenciesQueryOptions().queryKey })
    if (targetListId) {
      try {
        const library = await queryClient.fetchQuery(competenciesQueryOptions())
        const ids = library.competencies.filter((c) => imported.codes.includes(c.code)).map((c) => c.id)
        const list = await addCompetencies.mutateAsync({ listId: targetListId, data: { parentNodeId, competencyIds: ids, index: null } })
        queryClient.setQueryData(listQueryOptions(targetListId).queryKey, list)
        void queryClient.invalidateQueries({ queryKey: listsQueryOptions().queryKey })
      } catch {
        setFailure('addToList')
      }
    }
    setResult(imported)
  }

  return (
    <ImportProvider
      lists={lists.lists.filter((l) => l.isActive)}
      targetListId={targetListId}
      headings={target.data?.nodes.filter((n) => n.kind === 'Heading') ?? []}
      onTargetListChange={setTargetListId}
      busy={previewMutation.isPending || importMutation.isPending || addCompetencies.isPending}
      error={errorText()}
      preview={preview}
      result={result}
      onPreview={(csv) => {
        setFailure(null)
        previewMutation.mutate({ data: { csv } }, { onSuccess: setPreview, onError: () => setFailure('preview') })
      }}
      onImport={(csv, parentNodeId) => void importAll(csv, parentNodeId)}
      onReset={() => {
        setPreview(null)
        setResult(null)
        setFailure(null)
      }}
    >
      <ImportPage />
    </ImportProvider>
  )
}
