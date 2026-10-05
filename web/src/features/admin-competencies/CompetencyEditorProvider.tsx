import { createContext, type ReactNode, useContext, useState } from 'react'
import type { AdminCompetencyDto, RevisionContentRequest } from '@shared/api/model'
import { type EditorTab, tabOf, useCompetencyDraft } from './model/useCompetencyDraft'
import { useReviewFlow } from './model/useReviewFlow'

type Props = {
  competency: AdminCompetencyDto
  /** The list the editor was opened from, for the breadcrumb. */
  fromListId: string | null
  busy: boolean
  error: string | null
  /** What the last save did ("Saved to revision 3"), shown once after it. */
  notice: string | null
  // Each resolves true once saved.
  onSaveEdit: (content: RevisionContentRequest) => Promise<boolean>
  onPublish: (content: RevisionContentRequest, invalidatesPreviousReviews: boolean) => Promise<boolean>
  onSetActive: (isActive: boolean) => Promise<boolean>
  children: ReactNode
}

function useEditorState({ competency, ...props }: Omit<Props, 'children'>) {
  const draft = useCompetencyDraft(competency.current)
  const review = useReviewFlow(draft.changes)
  const [tab, setTab] = useState<EditorTab>('details')
  const [confirmingDeactivate, setConfirmingDeactivate] = useState(false)
  return { ...props, competency, draft, review, tab, setTab, confirmingDeactivate, setConfirmingDeactivate }
}

const CompetencyEditorContext = createContext<ReturnType<typeof useEditorState> | null>(null)

/**
 * The competency editor: one working copy edited across three tabs, saved through the "Publish changes"
 * review (edit or new revision). The container remounts this provider after each save, so the draft always
 * starts from the current revision. No data-layer dependency.
 */
export function CompetencyEditorProvider({ children, ...props }: Props) {
  return <CompetencyEditorContext.Provider value={useEditorState(props)}>{children}</CompetencyEditorContext.Provider>
}

function useEditor() {
  const context = useContext(CompetencyEditorContext)
  if (!context) throw new Error('Competency editor hooks must be used within a CompetencyEditorProvider')
  return context
}

export function useCompetencyHeader() {
  const { competency, fromListId, busy, error, notice, review, setConfirmingDeactivate, onSetActive } = useEditor()
  return {
    competency,
    fromList: competency.lists.find((list) => list.id === fromListId) ?? null,
    otherLists: competency.lists,
    busy,
    error: review.step ? null : error,
    notice,
    deactivate: () => setConfirmingDeactivate(true),
    reactivate: () => void onSetActive(true),
  }
}

export function useEditorTabs() {
  const { tab, setTab, draft } = useEditor()
  const changed = (t: EditorTab) => draft.changes.some((field) => tabOf[field] === t)
  return { tab, setTab, changed }
}

export function useDraft() {
  return useEditor().draft
}

export function useEditorFooter() {
  const { draft, review, busy } = useEditor()
  return { changeCount: draft.changes.length, canPublish: draft.changes.length > 0 && draft.valid && !busy, publish: review.start, discard: draft.discard }
}

export function useRevisionHistory() {
  const { competency } = useEditor()
  return { revisions: competency.revisions, lists: competency.lists, competencyId: competency.id, current: competency.current.number }
}

export function useReview() {
  const { competency, draft, review, busy, error, onSaveEdit, onPublish } = useEditor()
  const publish = async (invalidates: boolean) => {
    if (await onPublish(draft.toContent(), invalidates)) review.close()
  }
  return {
    ...review,
    competency,
    changes: draft.changes,
    busy,
    error,
    /** Review step: save as an edit, or go on to the sign-offs question. */
    next: async () => {
      if (review.saveAs === 'revision') return review.goTo('signoffs')
      if (await onSaveEdit(draft.toContent())) review.close()
    },
    /** Sign-offs step: keeping sign-offs publishes now; reassessment needs one more confirmation. */
    publishOrConfirm: () => {
      if (review.invalidates === false) return void publish(false)
      if (review.invalidates === true) review.goTo('confirm')
    },
    publishWithReassessment: () => void publish(true),
  }
}

export function useDeactivateConfirm() {
  const { competency, busy, confirmingDeactivate, setConfirmingDeactivate, onSetActive } = useEditor()
  return {
    open: confirmingDeactivate,
    competency,
    busy,
    cancel: () => setConfirmingDeactivate(false),
    confirm: async () => {
      if (await onSetActive(false)) setConfirmingDeactivate(false)
    },
  }
}
