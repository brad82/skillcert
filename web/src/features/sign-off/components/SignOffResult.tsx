import { msg } from '@lingui/core/macro'
import { Plural, Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { CircleCheck } from 'lucide-react'
import type { SignOffResultDto } from '@shared/api/model'
import type { CompetencyStatusKey } from '@shared/lib/theme'
import { useSignOffResult } from '../SignOffProvider'

const chipFor = (review: SignOffResultDto): { key: CompetencyStatusKey; label: ReturnType<typeof msg> } =>
  review.confirmationStatus === 'Pending'
    ? { key: 'pending', label: msg`Pending` }
    : review.outcome === 'Competent'
      ? { key: 'current', label: msg`Competent` }
      : { key: 'notCompetent', label: msg`Not competent` }

const resultStyles = () => ({
  item: { display: 'flex', alignItems: 'center', gap: 2, py: 2, borderBottom: 1, borderColor: 'divider' },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', minWidth: 44 }),
  title: { flexGrow: 1, minWidth: 0 },
  chip: (key: CompetencyStatusKey) => (theme: Theme) => ({ bgcolor: theme.palette.status[key].bg, color: theme.palette.status[key].fg, fontWeight: 600 }),
})

/** Back on the candidate's screen: what was recorded, and who still has to confirm (wireframe 5a). */
export function SignOffResult() {
  const { result, onDone } = useSignOffResult()
  const { i18n } = useLingui()
  const styles = resultStyles()
  const count = result.reviews.length
  const name = result.reviewerName
  const pending = result.reviews.some((r) => r.confirmationStatus === 'Pending')

  return (
    <Stack spacing={4}>
      <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center', pt: 6 }}>
        <CircleCheck size={48} aria-hidden />
        <Typography variant="h1">
          <Plural value={count} one="# review recorded" other="# reviews recorded" />
        </Typography>
        {pending && (
          <Typography color="text.secondary">
            <Trans>Waiting for {name} to confirm.</Trans>
          </Typography>
        )}
      </Stack>
      <Box>
        {result.reviews.map((review) => {
          const chip = chipFor(review)
          return (
            <Box key={review.reviewId} sx={styles.item}>
              <Typography component="span" sx={styles.code}>
                {review.code}
              </Typography>
              <Typography component="span" variant="body2" sx={styles.title}>
                {review.shortTitle}
              </Typography>
              <Chip size="small" label={i18n._(chip.label)} sx={styles.chip(chip.key)} />
            </Box>
          )
        })}
      </Box>
      <Button variant="contained" size="large" onClick={onDone}>
        <Trans>Back to basket</Trans>
      </Button>
    </Stack>
  )
}
