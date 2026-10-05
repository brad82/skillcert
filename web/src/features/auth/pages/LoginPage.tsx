import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { EmailField } from '../components/Form/EmailField'
import { PasswordField } from '../components/Form/PasswordField'
import { SubmitButton } from '../components/Form/SubmitButton'
import { LoginLayout } from '../components/Layout/LoginLayout'
import { useLoginForm } from '../LoginProvider'

export function LoginPage() {
  const { error, onSubmit } = useLoginForm()

  return (
    <LoginLayout>
      <Typography variant="h1" gutterBottom>
        <Trans>Sign in to SkillCert</Trans>
      </Typography>
      {/* noValidate: the Zod-backed model drives the messages, not the browser. */}
      <Stack component="form" spacing={2} onSubmit={onSubmit} noValidate>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <EmailField />
        <PasswordField />
        <SubmitButton />
      </Stack>
    </LoginLayout>
  )
}
