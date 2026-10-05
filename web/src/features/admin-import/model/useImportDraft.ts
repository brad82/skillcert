import { useState } from 'react'
import { type CsvFile, checkFile, type FileProblem } from './csvFile'

export type RowFilter = 'all' | 'errors' | 'warnings'

/**
 * The import screen's local state: the chosen file (read as text in the browser), the preview's row filter
 * and the heading to add to. Knows nothing about the API.
 */
export function useImportDraft() {
  const [file, setFile] = useState<CsvFile | null>(null)
  const [problem, setProblem] = useState<FileProblem>(null)
  const [filter, setFilter] = useState<RowFilter>('all')
  const [parentNodeId, setParentNodeId] = useState<string | null>(null)
  return {
    file,
    problem,
    filter,
    setFilter,
    parentNodeId,
    setParentNodeId,
    /** Resolves with the file's text when it's acceptable. */
    choose: async (chosen: File): Promise<CsvFile | null> => {
      const fileProblem = checkFile(chosen)
      setProblem(fileProblem)
      if (fileProblem) return null
      const read = { name: chosen.name, size: chosen.size, text: await chosen.text() }
      setFile(read)
      setFilter('all')
      return read
    },
    clear: () => {
      setFile(null)
      setProblem(null)
    },
  }
}

export type ImportDraft = ReturnType<typeof useImportDraft>
