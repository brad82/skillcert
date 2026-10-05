import { createFileRoute } from '@tanstack/react-router'
import { ApprovalsContainer, approvalsQueryOptions } from '@features/approvals'

export const Route = createFileRoute('/_authenticated/approvals')({
  loader: ({ context }) => context.queryClient.ensureQueryData(approvalsQueryOptions()),
  component: ApprovalsContainer,
})
