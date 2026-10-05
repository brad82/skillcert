import { createContext, type ReactNode, useContext } from 'react'
import type { AdminCompetencyListItemDto, AdminListDto, CreateCompetencyRequest } from '@shared/api/model'
import { childrenOf, competencyCountUnder, competencyIdsIn, headingTargets, siblingCount, visibleNodes } from './model/tree'
import { useAddExistingDraft } from './model/useAddExistingDraft'
import { useCollapsed } from './model/useCollapsed'
import { useEditorDialog } from './model/useEditorDialog'
import { useHeadingDraft } from './model/useHeadingDraft'
import { useListDetailsDraft } from './model/useListDetailsDraft'
import { useNewCompetencyDraft } from './model/useNewCompetencyDraft'

export type CreateOutcome = 'ok' | 'duplicate' | 'failed'

type Props = {
  list: AdminListDto
  /** Every competency, for the add-existing picker. */
  library: AdminCompetencyListItemDto[]
  busy: boolean
  error: string | null
  // Each change resolves true once saved, so the open dialog can close.
  onRenameList: (title: string, description: string | null) => Promise<boolean>
  onAddHeading: (parentId: string | null, code: string | null, title: string) => Promise<boolean>
  onRenameHeading: (nodeId: string, code: string | null, title: string) => Promise<boolean>
  onMove: (nodeId: string, parentId: string | null, index: number) => Promise<boolean>
  onRemove: (nodeId: string) => Promise<boolean>
  onAddCompetencies: (parentId: string | null, competencyIds: string[]) => Promise<boolean>
  /** Creates the competency, then adds it to this list under the parent. */
  onCreateCompetency: (parentId: string | null, request: CreateCompetencyRequest) => Promise<CreateOutcome>
  children: ReactNode
}

function useEditorState(props: Omit<Props, 'children'>) {
  return {
    ...props,
    dialogs: useEditorDialog(),
    collapsed: useCollapsed(),
    details: useListDetailsDraft(),
    heading: useHeadingDraft(),
    addExisting: useAddExistingDraft(),
    newCompetency: useNewCompetencyDraft(),
  }
}

const ListEditorContext = createContext<ReturnType<typeof useEditorState> | null>(null)

/**
 * The list tree editor's state: collapsed headings, the one open dialog and each dialog's draft, composed
 * with the tree changes passed in. Changes are live (no list revisions). No data-layer dependency.
 */
export function ListEditorProvider({ children, ...props }: Props) {
  return <ListEditorContext.Provider value={useEditorState(props)}>{children}</ListEditorContext.Provider>
}

function useEditor() {
  const context = useContext(ListEditorContext)
  if (!context) throw new Error('List editor hooks must be used within a ListEditorProvider')
  return context
}

/** The heading the add dialogs default to: none, so new items go at the top level. */
const defaultParent = null

export function useListHeader() {
  const { list, error, dialogs, details, heading, addExisting, newCompetency } = useEditor()
  return {
    list,
    error: dialogs.dialog ? null : error,
    rename: () => {
      details.reset(list.title, list.description)
      dialogs.open({ kind: 'renameList' })
    },
    addHeading: () => {
      heading.reset(defaultParent)
      dialogs.open({ kind: 'addHeading' })
    },
    addExisting: () => {
      addExisting.reset(defaultParent)
      dialogs.open({ kind: 'addExisting' })
    },
    newCompetency: () => {
      newCompetency.reset(defaultParent)
      dialogs.open({ kind: 'newCompetency' })
    },
  }
}

export function useTreeRows() {
  const { list, busy, collapsed, dialogs, heading, onMove } = useEditor()
  const nodes = list.nodes
  return {
    listId: list.id,
    busy,
    rows: visibleNodes(nodes, collapsed.collapsed).map((node) => ({
      node,
      collapsed: collapsed.collapsed.has(node.id),
      competencyCount: node.kind === 'Heading' ? competencyCountUnder(nodes, node.id) : 0,
      canMoveUp: node.index > 0,
      canMoveDown: node.index < siblingCount(nodes, node.parentNodeId) - 1,
      hasChildren: childrenOf(nodes, node.id).length > 0,
    })),
    toggle: collapsed.toggle,
    moveUp: (nodeId: string) => {
      const node = nodes.find((n) => n.id === nodeId)
      if (node) void onMove(nodeId, node.parentNodeId, node.index - 1)
    },
    moveDown: (nodeId: string) => {
      const node = nodes.find((n) => n.id === nodeId)
      if (node) void onMove(nodeId, node.parentNodeId, node.index + 1)
    },
    moveTo: (nodeId: string) => dialogs.open({ kind: 'move', nodeId }),
    rename: (nodeId: string) => {
      const node = nodes.find((n) => n.id === nodeId)
      heading.reset(node?.parentNodeId ?? null, node?.headingCode, node?.headingTitle)
      dialogs.open({ kind: 'renameHeading', nodeId })
    },
    remove: (nodeId: string) => dialogs.open({ kind: 'remove', nodeId }),
  }
}

/** The error from the last change, shown inside whichever dialog is open. */
function useDialogChrome() {
  const { dialogs, busy, error } = useEditor()
  return { busy, error, close: dialogs.close }
}

export function useRenameListDialog() {
  const { dialogs, details, onRenameList } = useEditor()
  return {
    ...useDialogChrome(),
    open: dialogs.dialog?.kind === 'renameList',
    draft: details,
    submit: async () => {
      if (details.canSubmit && (await onRenameList(details.title.trim(), details.description.trim() || null))) dialogs.close()
    },
  }
}

export function useHeadingDialog() {
  const { list, dialogs, heading, onAddHeading, onRenameHeading } = useEditor()
  const dialog = dialogs.dialog
  const renaming = dialog?.kind === 'renameHeading' ? dialog.nodeId : null
  return {
    ...useDialogChrome(),
    open: dialog?.kind === 'addHeading' || renaming !== null,
    renaming: renaming !== null,
    draft: heading,
    parents: headingTargets(list.nodes),
    submit: async () => {
      if (!heading.canSubmit) return
      const code = heading.code.trim() || null
      const title = heading.title.trim()
      const saved = renaming ? await onRenameHeading(renaming, code, title) : await onAddHeading(heading.parentId, code, title)
      if (saved) dialogs.close()
    },
  }
}

export function useMoveDialog() {
  const { list, dialogs, heading, onMove } = useEditor()
  const nodeId = dialogs.dialog?.kind === 'move' ? dialogs.dialog.nodeId : null
  const node = list.nodes.find((n) => n.id === nodeId) ?? null
  return {
    ...useDialogChrome(),
    open: node !== null,
    node,
    // The move dialog reuses the heading draft's parent field for its target.
    target: heading.parentId,
    setTarget: heading.setParentId,
    parents: nodeId ? headingTargets(list.nodes, nodeId) : [],
    submit: async () => {
      if (!node) return
      const index = siblingCount(
        list.nodes.filter((n) => n.id !== node.id),
        heading.parentId,
      )
      if (await onMove(node.id, heading.parentId, index)) dialogs.close()
    },
  }
}

export function useRemoveDialog() {
  const { list, dialogs, onRemove } = useEditor()
  const nodeId = dialogs.dialog?.kind === 'remove' ? dialogs.dialog.nodeId : null
  const node = list.nodes.find((n) => n.id === nodeId) ?? null
  return {
    ...useDialogChrome(),
    open: node !== null,
    node,
    listTitle: list.title,
    competencyCount: node ? competencyCountUnder(list.nodes, node.id) : 0,
    confirm: async () => {
      if (node && (await onRemove(node.id))) dialogs.close()
    },
  }
}

export function useAddExistingDialog() {
  const { list, library, dialogs, addExisting, onAddCompetencies } = useEditor()
  const inList = competencyIdsIn(list.nodes)
  const selectable = [...addExisting.selected].filter((id) => !inList.has(id))
  return {
    ...useDialogChrome(),
    open: dialogs.dialog?.kind === 'addExisting',
    listTitle: list.title,
    draft: addExisting,
    parents: headingTargets(list.nodes),
    results: addExisting.results(library).map((competency) => ({ competency, inList: inList.has(competency.id) })),
    count: selectable.length,
    submit: async () => {
      if (selectable.length > 0 && (await onAddCompetencies(addExisting.parentId, selectable))) dialogs.close()
    },
  }
}

export function useNewCompetencyDialog() {
  const { list, dialogs, newCompetency, addExisting, onCreateCompetency } = useEditor()
  return {
    ...useDialogChrome(),
    open: dialogs.dialog?.kind === 'newCompetency',
    listTitle: list.title,
    draft: newCompetency,
    parents: headingTargets(list.nodes),
    /** Switches to add-existing with the duplicate code already searched. */
    addExistingInstead: () => {
      addExisting.reset(newCompetency.parentId, newCompetency.code.trim())
      dialogs.open({ kind: 'addExisting' })
    },
    submit: async (): Promise<CreateOutcome | null> => {
      const request = newCompetency.submit()
      if (!request) return null
      const outcome = await onCreateCompetency(newCompetency.parentId, request)
      if (outcome === 'ok') dialogs.close()
      if (outcome === 'duplicate') newCompetency.markDuplicate()
      return outcome
    },
  }
}
