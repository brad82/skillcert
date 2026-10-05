import { useLingui } from '@lingui/react/macro'
import TextField from '@mui/material/TextField'
import { usePasswordField } from '../../LoginProvider'

export function PasswordField() {
  const { value, setValue, problem, showErrors } = usePasswordField()
  const { t } = useLingui()
  const showProblem = showErrors && problem !== null

  return (
    <TextField
      label={t`Password`}
      type="password"
      autoComplete="current-password"
      value={value}
      onChange={(event) => setValue(event.target.value)}
      error={showProblem}
      helperText={showProblem ? t`Password is required` : ' '}
      required
      fullWidth
    />
  )
}
