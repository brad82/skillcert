import { useLingui } from '@lingui/react/macro'
import TextField from '@mui/material/TextField'
import { useEmailField } from '../../LoginProvider'

export function EmailField() {
  const { value, setValue, problem, showErrors } = useEmailField()
  const { t } = useLingui()
  const showProblem = showErrors && problem !== null

  return (
    <TextField
      label={t`Email`}
      type="email"
      autoComplete="username"
      value={value}
      onChange={(event) => setValue(event.target.value)}
      error={showProblem}
      // " " keeps the helper row so the layout doesn't jump when an error appears.
      helperText={showProblem ? (problem === 'required' ? t`Email is required` : t`Enter a valid email address`) : ' '}
      required
      fullWidth
      autoFocus
    />
  )
}
