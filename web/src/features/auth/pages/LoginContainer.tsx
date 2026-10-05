import { useLingui } from '@lingui/react/macro'
import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { currentUserQueryOptions } from '@features/current-user'
import { isApiError } from '@shared/api/client'
import { useLogin } from '../api/authApi.gen'
import { LoginProvider } from '../LoginProvider'
import { LoginPage } from './LoginPage'

type Props = {
  /** Same-origin path to return to after sign-in. */
  redirectTo?: string
}

/**
 * The only place the sign-in mutation is called. Maps API failures to display copy and
 * navigates on success; the provider and page below only see plain values.
 */
export function LoginContainer({ redirectTo }: Props) {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const login = useLogin()

  function loginError(): string | null {
    if (!login.error) return null
    // 401 covers wrong password, locked and deactivated accounts; the API deliberately doesn't say which.
    if (isApiError(login.error, 401)) return t`Email or password is incorrect, or the account is locked or inactive.`
    return t`We couldn't sign you in. Please try again.`
  }

  return (
    <LoginProvider
      saving={login.isPending}
      error={loginError()}
      onLogin={(credentials) =>
        login.mutate(
          { data: credentials },
          {
            onSuccess: async () => {
              await queryClient.invalidateQueries({ queryKey: currentUserQueryOptions().queryKey })
              await navigate({ href: redirectTo ?? '/', replace: true })
            },
          },
        )
      }
    >
      <LoginPage />
    </LoginProvider>
  )
}
