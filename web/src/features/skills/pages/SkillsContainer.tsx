import { useSuspenseQuery } from '@tanstack/react-query'
import { myListsQueryOptions } from '@features/my-record'
import type { PresentationState } from '@shared/lib/competencyStatus'
import { SkillsProvider } from '../SkillsProvider'
import { SkillsPage } from './SkillsPage'

type Props = {
  status: PresentationState | undefined
  onClearStatus: () => void
  onOpenCompetency: (competencyId: string) => void
  onOpenBasket: () => void
}

/** Owns the my-lists query; navigation comes from the route as callbacks. */
export function SkillsContainer({ status, onClearStatus, onOpenCompetency, onOpenBasket }: Props) {
  const { data } = useSuspenseQuery(myListsQueryOptions())

  return (
    <SkillsProvider lists={data.lists} status={status} onClearStatus={onClearStatus} onOpenCompetency={onOpenCompetency} onOpenBasket={onOpenBasket}>
      <SkillsPage />
    </SkillsProvider>
  )
}
