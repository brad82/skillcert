import { useSuspenseQuery } from '@tanstack/react-query'
import { approvalsQueryOptions } from '@features/approvals'
import { useCurrentUser } from '@features/current-user'
import { countStates, distinctCompetencies, myListsQueryOptions } from '@features/my-record'
import { HomePage } from './HomePage'

type Props = {
  onOpenApprovals: () => void
}

/** Owns the my-lists and approvals queries; counts each competency once across lists. */
export function HomeContainer({ onOpenApprovals }: Props) {
  const { data } = useSuspenseQuery(myListsQueryOptions())
  const { data: approvals } = useSuspenseQuery(approvalsQueryOptions())
  const { displayName } = useCurrentUser()

  return (
    <HomePage
      firstName={displayName.split(' ')[0] ?? displayName}
      lists={data.lists}
      counts={countStates(distinctCompetencies(data.lists))}
      approvalsWaiting={approvals.groups.length}
      onOpenApprovals={onOpenApprovals}
    />
  )
}
