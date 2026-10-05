import { BasketPageProvider } from '../BasketPageProvider'
import { BasketPage } from './BasketPage'

type Props = {
  onSignOff: (competencyIds: string[]) => void
  onOpenSkills: () => void
}

/** The basket screen. No server data: the basket is client state until sign-off (spec §9). */
export function BasketContainer({ onSignOff, onOpenSkills }: Props) {
  return (
    <BasketPageProvider onSignOff={onSignOff} onOpenSkills={onOpenSkills}>
      <BasketPage />
    </BasketPageProvider>
  )
}
