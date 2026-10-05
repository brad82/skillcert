import { createContext, type ReactNode, useContext } from 'react'
import type { AdminListNodeDto, AdminListSummaryDto, CompetencyImportPreviewResponse, CompetencyImportResponse } from '@shared/api/model'
import { useImportDraft } from './model/useImportDraft'

type Props = {
  /** Lists the imported competencies can be added to. */
  lists: AdminListSummaryDto[]
  targetListId: string | null
  /** The target list's headings, once it has loaded. */
  headings: AdminListNodeDto[]
  onTargetListChange: (listId: string | null) => void
  busy: boolean
  error: string | null
  preview: CompetencyImportPreviewResponse | null
  result: CompetencyImportResponse | null
  onPreview: (csv: string) => void
  onImport: (csv: string, parentNodeId: string | null) => void
  /** Back to the upload step: forgets the preview and result. */
  onReset: () => void
  children: ReactNode
}

function useImportState(props: Omit<Props, 'children'>) {
  return { ...props, draft: useImportDraft() }
}

const ImportContext = createContext<ReturnType<typeof useImportState> | null>(null)

/**
 * CSV import (spec §26): upload → preview → import. Nothing is created until every row is valid, and all rows
 * are created together. Optionally adds the new competencies to a list. No data-layer dependency.
 */
export function ImportProvider({ children, ...props }: Props) {
  return <ImportContext.Provider value={useImportState(props)}>{children}</ImportContext.Provider>
}

function useImport() {
  const context = useContext(ImportContext)
  if (!context) throw new Error('Import hooks must be used within an ImportProvider')
  return context
}

export function useImportStep() {
  const { preview, result, error } = useImport()
  return { step: result ? ('done' as const) : preview ? ('preview' as const) : ('upload' as const), error }
}

export function useUpload() {
  const { draft, busy, onPreview, lists, targetListId, onTargetListChange, headings } = useImport()
  return {
    problem: draft.problem,
    busy,
    choose: async (file: File) => {
      const read = await draft.choose(file)
      if (read) onPreview(read.text)
    },
    lists,
    targetListId,
    setTargetListId: (id: string | null) => {
      draft.setParentNodeId(null)
      onTargetListChange(id)
    },
    headings,
    parentNodeId: draft.parentNodeId,
    setParentNodeId: draft.setParentNodeId,
  }
}

export function usePreview() {
  const { draft, preview, busy, lists, targetListId, headings, onImport, onReset } = useImport()
  const rows = preview?.rows ?? []
  const filtered = rows.filter((row) => draft.filter === 'all' || (draft.filter === 'errors' ? row.errors.length > 0 : row.warnings.length > 0))
  const heading = headings.find((h) => h.id === draft.parentNodeId) ?? null
  return {
    fileName: draft.file?.name ?? '',
    canImport: preview?.canImport ?? false,
    fileErrors: preview?.fileErrors ?? [],
    counts: {
      rows: rows.length,
      errors: rows.filter((r) => r.errors.length > 0).length,
      warnings: rows.filter((r) => r.warnings.length > 0).length,
      ready: rows.filter((r) => r.errors.length === 0).length,
    },
    filter: draft.filter,
    setFilter: draft.setFilter,
    rows: filtered,
    target: lists.find((l) => l.id === targetListId)?.title ?? null,
    targetHeading: heading ? [heading.headingCode, heading.headingTitle].filter(Boolean).join(' ') : null,
    busy,
    importAll: () => draft.file && onImport(draft.file.text, draft.parentNodeId),
    back: () => {
      draft.clear()
      onReset()
    },
  }
}

export function useImportResult() {
  const { result, lists, targetListId, draft, onReset } = useImport()
  return {
    result,
    target: lists.find((l) => l.id === targetListId) ?? null,
    another: () => {
      draft.clear()
      onReset()
    },
  }
}
