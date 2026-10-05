import { useSuspenseQuery } from '@tanstack/react-query'
import { archivedRecordUrl, currentRecordUrl, myArchivedRecordsQueryOptions, myListsQueryOptions } from '@features/my-record'
import { RecordsProvider } from '../RecordsProvider'
import { RecordsPage } from './RecordsPage'

/** Owns the lists and archived-records queries (both loaded by the route). */
export function RecordsContainer() {
  const { data: lists } = useSuspenseQuery(myListsQueryOptions())
  const { data: archives } = useSuspenseQuery(myArchivedRecordsQueryOptions())

  return (
    <RecordsProvider lists={lists.lists} archives={archives.records} currentRecordUrl={currentRecordUrl} archivedRecordUrl={archivedRecordUrl}>
      <RecordsPage />
    </RecordsProvider>
  )
}
