import Stack from '@mui/material/Stack'
import { BasketHeader } from '../components/BasketHeader'
import { EmptyBasket } from '../components/EmptyBasket'
import { GroupCard } from '../components/GroupCard'
import { MergeSuggestion } from '../components/MergeSuggestion'
import { useBasketGroupList } from '../BasketPageProvider'

/** Wireframe 4a: the basket split by who can sign. Layout only. */
export function BasketPage() {
  const { groups } = useBasketGroupList()

  return (
    <Stack spacing={4}>
      <BasketHeader />
      {groups.length === 0 ? (
        <EmptyBasket />
      ) : (
        <>
          <MergeSuggestion />
          {groups.map((group) => (
            <GroupCard key={group.level.order} group={group} />
          ))}
        </>
      )}
    </Stack>
  )
}
