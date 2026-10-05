import { Trans } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'
import { useBasketHeader } from '../BasketPageProvider'

const basketHeaderStyles = () => ({
  row: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 },
})

/** "Basket (N)" and CLEAR (wireframe 4a). */
export function BasketHeader() {
  const { count, clear } = useBasketHeader()
  const styles = basketHeaderStyles()

  return (
    <Box sx={styles.row}>
      <Typography variant="h1">
        <Trans>Basket ({count})</Trans>
      </Typography>
      {count > 0 && (
        <Button onClick={clear}>
          <Trans>Clear</Trans>
        </Button>
      )}
    </Box>
  )
}
