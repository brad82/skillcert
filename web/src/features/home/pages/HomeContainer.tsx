import { useSuspenseQuery } from '@tanstack/react-query'
import { useCurrentUser } from '@features/current-user'
import { countStates, distinctCompetencies, myListsQueryOptions } from '@features/my-record'
import { HomePage } from './HomePage'

/** Owns the my-lists query; counts each competency once across lists. */
export function HomeContainer() {
  const { data } = useSuspenseQuery(myListsQueryOptions())
  const { displayName } = useCurrentUser()

  return (
    <HomePage
      firstName={displayName.split(' ')[0] ?? displayName}
      lists={data.lists}
      counts={countStates(distinctCompetencies(data.lists))}
    />
  )
}
