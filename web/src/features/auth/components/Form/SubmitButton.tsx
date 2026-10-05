import { Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import { useLoginForm } from '../../LoginProvider'

export function SubmitButton() {
  const { saving } = useLoginForm()

  return (
    <Button type="submit" variant="contained" size="large" disabled={saving} fullWidth>
      {saving ? <Trans>Signing in…</Trans> : <Trans>Sign in</Trans>}
    </Button>
  )
}
