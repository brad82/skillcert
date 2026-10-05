import { useState } from 'react'

export type ListDetailsDraft = {
  title: string
  description: string
  setTitle: (title: string) => void
  setDescription: (description: string) => void
  /** Starts the draft from a list's current details, or empty for a new list. */
  reset: (title?: string, description?: string | null) => void
  canSubmit: boolean
}

/** A list's title (required) and description, for creating or renaming. Knows nothing about the API. */
export function useListDetailsDraft(): ListDetailsDraft {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  return {
    title,
    description,
    setTitle,
    setDescription,
    reset: (t = '', d = '') => {
      setTitle(t)
      setDescription(d ?? '')
    },
    canSubmit: title.trim().length > 0 && title.trim().length <= 200,
  }
}
