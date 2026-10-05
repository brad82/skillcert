import { useState } from 'react'

export type HeadingDraft = {
  parentId: string | null
  code: string
  title: string
  setParentId: (id: string | null) => void
  setCode: (code: string) => void
  setTitle: (title: string) => void
  reset: (parentId: string | null, code?: string | null, title?: string | null) => void
  canSubmit: boolean
}

/** A heading's code (optional, e.g. "4.1"), title (required) and, when adding, where it goes. */
export function useHeadingDraft(): HeadingDraft {
  const [parentId, setParentId] = useState<string | null>(null)
  const [code, setCode] = useState('')
  const [title, setTitle] = useState('')
  return {
    parentId,
    code,
    title,
    setParentId,
    setCode,
    setTitle,
    reset: (parent, c, t) => {
      setParentId(parent)
      setCode(c ?? '')
      setTitle(t ?? '')
    },
    canSubmit: title.trim().length > 0,
  }
}
