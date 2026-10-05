import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import type { Theme } from '@mui/material/styles'
import { Hourglass, Minus, Plus, RotateCcw, type LucideIcon } from 'lucide-react'
import type { BasketActionState } from '../model/competencyFacts'
import { useBasketAction } from '../CompetencyProvider'

const appearance: Record<BasketActionState, { label: MessageDescriptor; icon: LucideIcon; variant: 'contained' | 'outlined' }> = {
  add: { label: msg`Add to basket`, icon: Plus, variant: 'contained' },
  reassess: { label: msg`Add to basket for reassessment`, icon: RotateCcw, variant: 'outlined' },
  remove: { label: msg`Remove from basket`, icon: Minus, variant: 'outlined' },
  waiting: { label: msg`Waiting for confirmation`, icon: Hourglass, variant: 'outlined' },
}

const basketActionStyles = () => ({
  dock: (theme: Theme) => ({
    position: 'fixed',
    left: 0,
    right: 0,
    bottom: 'calc(56px + env(safe-area-inset-bottom, 0px))',
    px: 4,
    py: 3,
    bgcolor: 'background.default',
    borderTop: 1,
    borderColor: 'divider',
    zIndex: theme.zIndex.appBar - 1,
  }),
  button: { display: 'flex', width: '100%', maxWidth: 688, mx: 'auto' },
})

/** The bottom action (wireframe 3b), docked above the bottom nav. */
export function BasketAction() {
  const { state, onClick } = useBasketAction()
  const { i18n } = useLingui()
  const styles = basketActionStyles()
  const { label, icon: Icon, variant } = appearance[state]

  return (
    <Box sx={styles.dock}>
      <Button
        variant={variant}
        size="large"
        disabled={state === 'waiting'}
        startIcon={<Icon size={18} aria-hidden />}
        onClick={onClick}
        sx={styles.button}
      >
        {i18n._(label)}
      </Button>
    </Box>
  )
}
