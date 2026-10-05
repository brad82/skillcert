import { Trans } from '@lingui/react/macro'
import Step from '@mui/material/Step'
import StepLabel from '@mui/material/StepLabel'
import Stepper from '@mui/material/Stepper'
import { useImportStep } from '../ImportProvider'

const order = { upload: 0, preview: 1, done: 2 }

export function ImportSteps() {
  const { step } = useImportStep()
  return (
    <Stepper activeStep={order[step]} sx={{ maxWidth: 560 }}>
      <Step><StepLabel><Trans>Upload</Trans></StepLabel></Step>
      <Step><StepLabel><Trans>Preview</Trans></StepLabel></Step>
      <Step completed={step === 'done'}><StepLabel><Trans>Import</Trans></StepLabel></Step>
    </Stepper>
  )
}
