import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import ButtonBase from '@mui/material/ButtonBase'
import Checkbox from '@mui/material/Checkbox'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { toBasketItem, useBasket } from '@features/basket'
import type { MyCompetencySummaryDto } from '@shared/api/model'
import { StatusChip } from '@shared/components/StatusChip'
import { canAddToBasket } from '@shared/lib/competencyStatus'
import { useOpenCompetency } from '../../SkillsProvider'

type Props = {
  competency: MyCompetencySummaryDto
  depth: number
}

const competencyRowStyles = (depth: number) => ({
  row: { display: 'flex', alignItems: 'center', gap: 1, pl: 1 + depth * 3, pr: 2, minHeight: 52, borderBottom: 1, borderColor: 'divider' },
  open: { flexGrow: 1, minWidth: 0, display: 'flex', alignItems: 'center', gap: 2, py: 2, textAlign: 'left', justifyContent: 'flex-start' },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', flexShrink: 0, minWidth: 44 }),
  title: { flexGrow: 1, minWidth: 0 },
})

/** A competency leaf: basket checkbox, then the row opens the detail screen (wireframe 2a). */
export function CompetencyRow({ competency, depth }: Props) {
  const basket = useBasket()
  const open = useOpenCompetency()
  const { t } = useLingui()
  const inBasket = basket.has(competency.competencyId)
  const addable = canAddToBasket(competency.currency)
  const styles = competencyRowStyles(depth)

  return (
    <Box sx={styles.row}>
      <Checkbox
        checked={inBasket}
        disabled={!addable && !inBasket}
        onChange={() => basket.toggle(toBasketItem(competency))}
        slotProps={{ input: { 'aria-label': t`Add ${competency.code} to basket` } }}
      />
      <ButtonBase sx={styles.open} onClick={() => open(competency.competencyId)}>
        <Typography component="span" sx={styles.code}>
          {competency.code}
        </Typography>
        <Typography component="span" variant="body2" sx={styles.title}>
          {competency.shortTitle}
        </Typography>
        <StatusChip currency={competency.currency} />
      </ButtonBase>
    </Box>
  )
}
