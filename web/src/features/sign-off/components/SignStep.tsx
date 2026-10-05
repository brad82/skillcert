import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useRef, useState } from 'react'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useSignStep } from '../SignOffProvider'
import { SignaturePadCanvas } from './SignaturePadCanvas'

/** Reviewer step 3: the confirmation statement, the signature pad, and "Submit & hand back" (wireframe 5a). */
export function SignStep() {
  const { candidateName, reviewer, counts, pad, signatureRequired, canSubmit, submitting, error, back, submit } = useSignStep()
  const { i18n } = useLingui()
  const { locale } = useLocale()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const name = reviewer.displayName
  const method = i18n._(reviewerLabel(reviewer.method, reviewer.classificationCode))
  const competent = counts.competent
  const notCompetent = counts.notCompetent
  const [now] = useState(() => new Date().toISOString())
  const today = formatDate(now, locale)

  return (
    <Stack spacing={4}>
      <Typography variant="h1">
        <Trans>Sign</Trans>
      </Typography>
      <Typography>
        <Trans>
          {name} ({method}) confirms {competent} Competent, {notCompetent} Not competent for {candidateName}, {today}.
        </Trans>
      </Typography>
      {reviewer.needsConfirmation && (
        <Alert severity="info">
          <Trans>These sign-offs count once {name} confirms them in SkillCert.</Trans>
        </Alert>
      )}
      {signatureRequired && (
        <Box>
          <SignaturePadCanvas canvasRef={canvasRef} />
          <Button size="small" onClick={pad.clear} disabled={!pad.hasInk} sx={{ mt: 1 }}>
            <Trans>Clear signature</Trans>
          </Button>
        </Box>
      )}
      {error && <Alert severity="error">{error}</Alert>}
      <Stack direction="row" spacing={2}>
        <Button onClick={back} disabled={submitting}>
          <Trans>Back</Trans>
        </Button>
        <Button
          variant="contained"
          size="large"
          disabled={!canSubmit}
          onClick={() => submit(canvasRef.current?.clientWidth ?? 0, canvasRef.current?.clientHeight ?? 0)}
          sx={{ flexGrow: 1 }}
        >
          {submitting ? <Trans>Submitting…</Trans> : <Trans>Submit & hand back</Trans>}
        </Button>
      </Stack>
    </Stack>
  )
}
