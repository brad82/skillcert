import { queryOptions } from '@tanstack/react-query'
import {
  getGetMyArchivedRecordsQueryKey,
  getGetMyArchivedRecordUrl,
  getGetMyCurrentRecordUrl,
  getMyArchivedRecords,
} from '../api/myRecordApi.gen'

/** The candidate's archived training records, newest first. */
export const myArchivedRecordsQueryOptions = () =>
  queryOptions({
    queryKey: getGetMyArchivedRecordsQueryKey(),
    queryFn: ({ signal }) => getMyArchivedRecords({ signal }),
    staleTime: 60_000,
  })

/** Download URL for the current record of a list (rendered now, never archived). */
export const currentRecordUrl = (listId: string) => getGetMyCurrentRecordUrl(listId)

/** Download URL for one archived record PDF. */
export const archivedRecordUrl = (recordId: string) => getGetMyArchivedRecordUrl(recordId)
