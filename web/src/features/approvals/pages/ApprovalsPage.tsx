import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { ApprovalCard } from '../components/ApprovalCard'
import { RejectDialog } from '../components/RejectDialog'
import { useApprovalGroups } from '../ApprovalsProvider'

/** The reviewer's confirmation queue: one card per sitting, oldest first. Layout only. */
export function ApprovalsPage() {
  const { groups, error } = useApprovalGroups()

  return (
    <Stack spacing={4}>
      <Typography variant="h1">
        <Trans>Waiting for you</Trans>
      </Typography>
      <Typography color="text.secondary">
        <Trans>Sign-offs you gave as a supervisor count once you confirm them here.</Trans>
      </Typography>
      {error && <Alert severity="warning">{error}</Alert>}
      {groups.length === 0 ? (
        <Typography color="text.secondary">
          <Trans>Nothing is waiting for your confirmation.</Trans>
        </Typography>
      ) : (
        groups.map((group) => <ApprovalCard key={group.signatureId} group={group} />)
      )}
      <RejectDialog />
    </Stack>
  )
}
