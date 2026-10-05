import { createContext, type ReactNode, useContext } from 'react'
import type { MyArchivedRecordDto, MyListDto } from '@shared/api/model'

type Props = {
  lists: MyListDto[]
  archives: MyArchivedRecordDto[]
  currentRecordUrl: (listId: string) => string
  archivedRecordUrl: (recordId: string) => string
  children: ReactNode
}

type RecordsContextValue = Omit<Props, 'children'>

const RecordsContext = createContext<RecordsContextValue | null>(null)

/** The Records tab's data and download links; no data-layer dependency. */
export function RecordsProvider({ children, ...value }: Props) {
  return <RecordsContext.Provider value={value}>{children}</RecordsContext.Provider>
}

function useRecordsContext() {
  const context = useContext(RecordsContext)
  if (!context) throw new Error('Records hooks must be used within a RecordsProvider')
  return context
}

/** Each required list with where to download its current record. */
export function useCurrentRecords() {
  const { lists, currentRecordUrl } = useRecordsContext()
  return lists.map((list) => ({ list, downloadUrl: currentRecordUrl(list.id) }))
}

export function useArchivedRecords() {
  const { archives, archivedRecordUrl } = useRecordsContext()
  return archives.map((archive) => ({ archive, downloadUrl: archivedRecordUrl(archive.id) }))
}
