import { Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { ShoppingBasket } from 'lucide-react'
import { useBasketGroupList } from '../BasketPageProvider'

export function EmptyBasket() {
  const { onOpenSkills } = useBasketGroupList()

  return (
    <Stack spacing={3} sx={{ alignItems: 'center', textAlign: 'center', pt: 8, color: 'text.secondary' }}>
      <ShoppingBasket size={40} aria-hidden />
      <Typography>
        <Trans>Your basket is empty. Tick skills in the Skills tab to sign them off together.</Trans>
      </Typography>
      <Button variant="outlined" onClick={onOpenSkills}>
        <Trans>Go to skills</Trans>
      </Button>
    </Stack>
  )
}
