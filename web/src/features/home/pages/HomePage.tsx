import { Trans } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { StateCounts } from '@features/my-record'
import type { MyListDto } from '@shared/api/model'
import { ComplianceCard } from '@shared/components/ComplianceCard'
import { ApprovalsBanner } from '../components/ApprovalsBanner'
import { NextEventCard } from '../components/NextEventCard'
import { StatusTiles } from '../components/StatusTiles'

type Props = {
  firstName: string
  lists: MyListDto[]
  counts: StateCounts
  approvalsWaiting: number
  onOpenApprovals: () => void
}

/** Wireframe 1b: compliance per list, status tiles, next event; plus claims waiting for this user to confirm. Layout only. */
export function HomePage({ firstName, lists, counts, approvalsWaiting, onOpenApprovals }: Props) {
  return (
    <Stack spacing={4}>
      <Typography variant="h1">
        <Trans>Hi {firstName}</Trans>
      </Typography>
      <ApprovalsBanner count={approvalsWaiting} onOpen={onOpenApprovals} />
      {lists.length === 0 ? (
        <Typography color="text.secondary">
          <Trans>You have no required skills records yet. An administrator adds you to a group to assign one.</Trans>
        </Typography>
      ) : (
        <>
          {lists.map((list) => (
            <ComplianceCard key={list.id} list={list} />
          ))}
          <StatusTiles counts={counts} />
        </>
      )}
      <NextEventCard />
    </Stack>
  )
}
