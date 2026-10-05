import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { DoneStep } from '../components/DoneStep'
import { ImportSteps } from '../components/ImportSteps'
import { PreviewStep } from '../components/PreviewStep'
import { UploadStep } from '../components/UploadStep'
import { useImportStep } from '../ImportProvider'

/** CSV import: the three steps, one at a time. Layout only. */
export function ImportPage() {
  const { step, error } = useImportStep()
  return (
    <Stack spacing={6}>
      <div>
        <Typography variant="h1"><Trans>Import competencies from CSV</Trans></Typography>
        <Typography variant="body2" color="text.secondary"><Trans>Each row becomes a new competency at revision 1. All rows import together, or none do.</Trans></Typography>
      </div>
      <ImportSteps />
      {error && <Alert severity="error">{error}</Alert>}
      {step === 'upload' && <UploadStep />}
      {step === 'preview' && <PreviewStep />}
      {step === 'done' && <DoneStep />}
    </Stack>
  )
}
