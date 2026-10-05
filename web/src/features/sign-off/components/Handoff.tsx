import { Plural, Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { ArrowRight, Smartphone } from 'lucide-react'
import { reviewLevelLabel } from '@shared/lib/reviewers'
import { useHandoff } from '../SignOffProvider'

/** Step 1, still the candidate's screen: "Pass your phone to …" (wireframe 5a). */
export function Handoff() {
  const { candidateName, count, level, startReview, onCancel } = useHandoff()
  const { i18n } = useLingui()
  const levelText = level ? i18n._(reviewLevelLabel(level)) : ''

  return (
    <Stack spacing={6} sx={{ alignItems: 'center', textAlign: 'center', pt: 12, px: 4 }}>
      <Smartphone size={48} aria-hidden />
      <Typography variant="h1">
        <Trans>Pass your phone to your reviewer</Trans>
      </Typography>
      <Typography color="text.secondary">
        <Trans>{levelText} or higher</Trans>
      </Typography>
      <Typography>
        {candidateName} · <Plural value={count} one="# skill" other="# skills" />
      </Typography>
      <Stack spacing={2} sx={{ width: '100%', maxWidth: 360 }}>
        <Button variant="contained" size="large" endIcon={<ArrowRight size={18} aria-hidden />} onClick={startReview}>
          <Trans>I'm the reviewer</Trans>
        </Button>
        <Button onClick={onCancel}>
          <Trans>Cancel</Trans>
        </Button>
      </Stack>
    </Stack>
  )
}
