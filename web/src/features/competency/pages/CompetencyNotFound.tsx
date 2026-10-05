import { Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { SearchX } from 'lucide-react'

type Props = {
  onBack: () => void
}

/** Shown when the skill isn't in any of the candidate's required lists (or doesn't exist). */
export function CompetencyNotFound({ onBack }: Props) {
  return (
    <Stack spacing={3} sx={{ alignItems: 'center', textAlign: 'center', pt: 12, color: 'text.secondary' }}>
      <SearchX size={40} aria-hidden />
      <Typography variant="h1" color="text.primary">
        <Trans>Skill not found</Trans>
      </Typography>
      <Typography>
        <Trans>This skill isn't part of your required records.</Trans>
      </Typography>
      <Button variant="outlined" onClick={onBack}>
        <Trans>Back to skills</Trans>
      </Button>
    </Stack>
  )
}
