import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import IconButton from '@mui/material/IconButton'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { ChevronLeft } from 'lucide-react'
import { StatusChip } from '@shared/components/StatusChip'
import { useCompetencyHeader } from '../../CompetencyProvider'

const detailHeaderStyles = () => ({
  row: { display: 'flex', alignItems: 'flex-start', gap: 1, ml: -2 },
  back: { mt: -1 },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', display: 'block' }),
  status: { mt: 3, ml: 10 },
})

/** "‹ code, short title" and the status pinned above the tabs (wireframe 3b). */
export function DetailHeader() {
  const { code, shortTitle, currency, onBack } = useCompetencyHeader()
  const { t } = useLingui()
  const styles = detailHeaderStyles()

  return (
    <Box>
      <Box sx={styles.row}>
        <IconButton aria-label={t`Back to skills`} onClick={onBack} sx={styles.back}>
          <ChevronLeft size={24} aria-hidden />
        </IconButton>
        <Box>
          <Typography component="span" sx={styles.code}>
            {code}
          </Typography>
          <Typography variant="h1">{shortTitle}</Typography>
        </Box>
      </Box>
      <Box sx={styles.status}>
        <StatusChip currency={currency} />
      </Box>
    </Box>
  )
}
