import { createFileRoute } from '@tanstack/react-router'
import { BasketContainer } from '@features/basket'

export const Route = createFileRoute('/_authenticated/basket')({
  component: BasketRoute,
})

function BasketRoute() {
  const navigate = Route.useNavigate()
  return (
    <BasketContainer
      onSignOff={(ids) => navigate({ to: '/sign-off', search: { ids } })}
      onOpenSkills={() => navigate({ to: '/skills' })}
    />
  )
}
