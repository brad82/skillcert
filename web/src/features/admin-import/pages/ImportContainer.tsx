import { useLingui } from '@lingui/react/macro'
import { useQuery, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { competenciesQueryOptions } from '@features/admin-competencies'
import { listQueryOptions, listsQueryOptions } from '@features/admin-lists'
import { isApiError, problemType } from '@shared/api/client'
import type { CompetencyImportPreviewResponse, CompetencyImportResponse } from '@shared/api/model'
import { useImportCompetencies, usePreviewCompetencyImport } from '../api/adminImportApi.gen'
import { ImportProvider } from '../ImportProvider'
import { ImportPage } from './ImportPage'

const isPreview = (body: unknown): body is CompetencyImportPreviewResponse => typeof body === 'object' && body !== null && 'rows' in body && 'canImport' in body

/**
 * Owns the preview and import mutations, and the optional target list. With a list chosen, the server creates
 * the competencies and places them in it in one transaction (the preview checks the list too). A 422 carries a
 * fresh preview, which replaces the shown one; nothing was created.
 */
export function ImportContainer() {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const { data: lists } = useSuspenseQuery(listsQueryOptions())
  const [targetListId, setTargetListId] = useState<string | null>(null)
  const target = useQuery({ ...listQueryOptions(targetListId ?? ''), enabled: targetListId !== null })
  const previewMutation = usePreviewCompetencyImport()
  const importMutation = useImportCompetencies()
  const [preview, setPreview] = useState<CompetencyImportPreviewResponse | null>(null)
  const [result, setResult] = useState<CompetencyImportResponse | null>(null)
  const [failure, setFailure] = useState<'preview' | 'conflict' | 'import' | null>(null)

  function errorText(): string | null {
    switch (failure) {
      case 'preview':
        return t`The file couldn't be checked. Please try again.`
      case 'conflict':
        return t`Someone created one of these codes while you were previewing. Go back and upload the file again.`
      case 'import':
        return t`The import failed and nothing was created. Please try again.`
      default:
        return null
    }
  }

  const placement = (parentNodeId: string | null) => (targetListId ? { listId: targetListId, parentNodeId, index: null } : null)

  async function importAll(csv: string, parentNodeId: string | null) {
    setFailure(null)
    let imported: CompetencyImportResponse
    try {
      imported = await importMutation.mutateAsync({ data: { csv, placement: placement(parentNodeId) } })
    } catch (error) {
      if (isApiError(error, 422) && isPreview(error.body)) return setPreview(error.body)
      return setFailure(problemType(error) === 'import.conflict' ? 'conflict' : 'import')
    }
    await queryClient.invalidateQueries({ queryKey: competenciesQueryOptions().queryKey })
    if (targetListId) {
      void queryClient.invalidateQueries({ queryKey: listQueryOptions(targetListId).queryKey })
      void queryClient.invalidateQueries({ queryKey: listsQueryOptions().queryKey })
    }
    setResult(imported)
  }

  return (
    <ImportProvider
      lists={lists.lists.filter((l) => l.isActive)}
      targetListId={targetListId}
      headings={target.data?.nodes.filter((n) => n.kind === 'Heading') ?? []}
      onTargetListChange={setTargetListId}
      busy={previewMutation.isPending || importMutation.isPending}
      error={errorText()}
      preview={preview}
      result={result}
      onPreview={(csv) => {
        setFailure(null)
        previewMutation.mutate({ data: { csv, placement: placement(null) } }, { onSuccess: setPreview, onError: () => setFailure('preview') })
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
