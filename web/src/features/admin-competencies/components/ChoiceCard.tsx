import Chip from '@mui/material/Chip'
import FormControlLabel from '@mui/material/FormControlLabel'
import Radio from '@mui/material/Radio'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

type Props = {
  value: string
  title: ReactNode
  description: ReactNode
  badge?: ReactNode
  disabled?: boolean
  selected: boolean
}

const choiceCardStyles = () => ({
  card: (theme: Theme) => ({ m: 0, p: 4, alignItems: 'flex-start', border: 1, borderColor: theme.palette.lineStrong, borderRadius: theme.radius.md, flex: 1 }),
  selected: (theme: Theme) => ({ borderWidth: 2, borderColor: theme.palette.primary.main, bgcolor: theme.palette.brandTint }),
})

/** A radio drawn as a card: title, optional "Recommended" badge, and why you'd choose it. Inside a RadioGroup. */
export function ChoiceCard({ value, title, description, badge, disabled, selected }: Props) {
  const styles = choiceCardStyles()
  return (
    <FormControlLabel
      value={value}
      disabled={disabled}
      control={<Radio sx={{ mt: -1 }} />}
      sx={[styles.card, selected && styles.selected]}
      label={
        <Stack spacing={1}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <Typography sx={{ fontWeight: 600 }}>{title}</Typography>
            {badge && <Chip size="small" color="primary" variant="outlined" label={badge} />}
          </Stack>
          <Typography variant="body2">{description}</Typography>
        </Stack>
      }
    />
  )
}
