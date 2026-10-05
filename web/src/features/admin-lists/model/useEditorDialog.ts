import { useState } from 'react'

export type EditorDialog =
  | { kind: 'renameList' }
  | { kind: 'addHeading' }
  | { kind: 'renameHeading'; nodeId: string }
  | { kind: 'move'; nodeId: string }
  | { kind: 'remove'; nodeId: string }
  | { kind: 'addExisting' }
  | { kind: 'newCompetency' }

/** Which of the list editor's dialogs is open, if any. Only one at a time. */
export function useEditorDialog() {
  const [dialog, setDialog] = useState<EditorDialog | null>(null)
  return { dialog, open: setDialog, close: () => setDialog(null) }
}
