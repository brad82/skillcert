import { Plural, Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { useBasket } from '@features/basket'
import { useOpenBasket } from '../SkillsProvider'

const basketBarStyles = () => ({
  bar: (theme: Theme) => ({
    position: 'fixed',
    left: 16,
    right: 16,
    bottom: 'calc(56px + 12px + env(safe-area-inset-bottom, 0px))',
    display: 'flex',
    alignItems: 'center',
    gap: 3,
    px: 4,
    py: 2,
    borderRadius: theme.radius.md,
    bgcolor: theme.palette.text.primary,
    color: theme.palette.background.paper,
    zIndex: theme.zIndex.snackbar,
    maxWidth: 688,
    mx: 'auto',
  }),
  text: { flexGrow: 1 },
  action: (theme: Theme) => ({ color: theme.palette.mode === 'light' ? '#ff9aa8' : theme.palette.primary.dark, fontWeight: 700 }),
})

/** "N in basket · REVIEW" above the bottom nav while the basket has items (wireframe 2a). */
export function BasketBar() {
  const { count } = useBasket()
  const openBasket = useOpenBasket()
  const styles = basketBarStyles()
  if (count === 0) return null

  return (
    <Paper role="status" elevation={0} sx={styles.bar}>
      <Typography sx={styles.text}>
        <Plural value={count} one="# in basket" other="# in basket" />
      </Typography>
      <Button onClick={openBasket} sx={styles.action}>
        <Trans>Review</Trans>
      </Button>
    </Paper>
  )
}
