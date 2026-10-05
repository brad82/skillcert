import { queryOptions } from '@tanstack/react-query'
import { getApprovals, getGetApprovalSignatureUrl, getGetApprovalsQueryKey } from '../api/approvalsApi.gen'

/** Pending claims naming the signed-in user, grouped by sitting. Invalidate after each decision. */
export const approvalsQueryOptions = () =>
  queryOptions({
    queryKey: getGetApprovalsQueryKey(),
    queryFn: ({ signal }) => getApprovals({ signal }),
    staleTime: 30_000,
  })

export const approvalSignatureUrl = (signatureId: string) => getGetApprovalSignatureUrl(signatureId)
