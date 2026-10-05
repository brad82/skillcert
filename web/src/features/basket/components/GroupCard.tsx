import { Plural, Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import IconButton from '@mui/material/IconButton'
import Paper from '@mui/material/Paper'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { PenLine, X } from 'lucide-react'
import { reviewLevelLabel } from '@shared/lib/reviewers'
import type { BasketGroup } from '../model/basketGroups'
import { useBasketGroupList } from '../BasketPageProvider'

type Props = {
  group: BasketGroup
}

const groupCardStyles = () => ({
  card: (theme: Theme) => ({ border: 1, borderColor: 'divider', borderRadius: theme.radius.md, overflow: 'hidden' }),
  title: { px: 4, pt: 4, pb: 2 },
  item: { display: 'flex', alignItems: 'center', gap: 2, pl: 4, pr: 1, minHeight: 48, borderTop: 1, borderColor: 'divider' },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', flexShrink: 0, minWidth: 44 }),
  itemTitle: { flexGrow: 1, minWidth: 0 },
  action: { p: 4, borderTop: 1, borderColor: 'divider' },
})

/** One reviewer-level group: "<level> or higher", its items with remove, and "Sign off these N". */
export function GroupCard({ group }: Props) {
  const { onSignOff, remove } = useBasketGroupList()
  const { i18n, t } = useLingui()
  const styles = groupCardStyles()
  const level = i18n._(reviewLevelLabel(group.level))
  const count = group.items.length

  return (
    <Paper component="section" elevation={0} sx={styles.card} aria-label={t`${level} or higher`}>
      <Typography variant="h2" sx={styles.title}>
        <Trans>{level} or higher</Trans>
      </Typography>
      {group.items.map((item) => (
        <Box key={item.competencyId} sx={styles.item}>
          <Typography component="span" sx={styles.code}>
            {item.code}
          </Typography>
          <Typography component="span" variant="body2" sx={styles.itemTitle}>
            {item.title}
          </Typography>
          <IconButton aria-label={t`Remove ${item.code}`} onClick={() => remove(item.competencyId)}>
            <X size={18} aria-hidden />
          </IconButton>
        </Box>
      ))}
      <Box sx={styles.action}>
        <Button variant="contained" fullWidth startIcon={<PenLine size={18} aria-hidden />} onClick={() => onSignOff(group)}>
          <Plural value={count} one="Sign off this one" other="Sign off these #" />
        </Button>
      </Box>
    </Paper>
  )
}
