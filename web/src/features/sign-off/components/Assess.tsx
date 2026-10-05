import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import TextField from '@mui/material/TextField'
import ToggleButton from '@mui/material/ToggleButton'
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup'
import Typography from '@mui/material/Typography'
import { useAssessment } from '../SignOffProvider'

const assessStyles = () => ({
  item: { display: 'flex', alignItems: 'center', gap: 2, py: 2, borderBottom: 1, borderColor: 'divider', flexWrap: 'wrap' },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', minWidth: 44 }),
  title: { flexGrow: 1, minWidth: 160 },
  notCompetent: (theme: Theme) => ({
    '&.Mui-selected': { bgcolor: theme.palette.status.notCompetent.bg, color: theme.palette.status.notCompetent.fg },
  }),
  competent: (theme: Theme) => ({
    '&.Mui-selected': { bgcolor: theme.palette.status.current.bg, color: theme.palette.status.current.fg },
  }),
})

/** Reviewer step 2: every item defaults to Competent; flip the exceptions (wireframe 5a). */
export function Assess() {
  const { candidateName, items, outcomes, toggle, comment, setComment, next, back } = useAssessment()
  const { t } = useLingui()
  const styles = assessStyles()

  return (
    <Stack spacing={4}>
      <Typography variant="h1">
        <Trans>Assess {candidateName}</Trans>
      </Typography>
      <Box>
        {items.map((item) => (
          <Box key={item.competencyId} sx={styles.item}>
            <Typography component="span" sx={styles.code}>
              {item.code}
            </Typography>
            <Typography component="span" variant="body2" sx={styles.title}>
              {item.title}
            </Typography>
            <ToggleButtonGroup
              exclusive
              size="small"
              value={outcomes[item.competencyId]}
              onChange={(_, value) => value && value !== outcomes[item.competencyId] && toggle(item.competencyId)}
              aria-label={t`Outcome for ${item.code}`}
            >
              <ToggleButton value="Competent" sx={styles.competent}>
                <Trans>Competent</Trans>
              </ToggleButton>
              <ToggleButton value="NotCompetent" sx={styles.notCompetent}>
                <Trans>Not competent</Trans>
              </ToggleButton>
            </ToggleButtonGroup>
          </Box>
        ))}
      </Box>
      <TextField label={t`Comment (optional)`} value={comment} onChange={(event) => setComment(event.target.value)} multiline minRows={2} slotProps={{ htmlInput: { maxLength: 1000 } }} />
      <Stack direction="row" spacing={2}>
        <Button onClick={back}>
          <Trans>Back</Trans>
        </Button>
        <Button variant="contained" onClick={next} sx={{ flexGrow: 1 }}>
          <Trans>Continue to sign</Trans>
        </Button>
      </Stack>
    </Stack>
  )
}
