import { createContext, type FormEvent, type ReactNode, useContext } from 'react'
import { type Credentials, type LoginDraft, useLoginDraft } from './model/useLoginDraft'

type Props = {
  saving: boolean
  error: string | null
  onLogin: (credentials: Credentials) => void
  children: ReactNode
}

type LoginContextValue = Omit<Props, 'children'> & { draft: LoginDraft }

const LoginContext = createContext<LoginContextValue | null>(null)

/**
 * Composes the sign-in model hooks and exposes them through named selector hooks.
 * No data-layer dependency: the container passes saving/error/onLogin as plain values.
 */
export function LoginProvider({ children, ...props }: Props) {
  const draft = useLoginDraft()
  return <LoginContext.Provider value={{ ...props, draft }}>{children}</LoginContext.Provider>
}

function useLoginContext() {
  const context = useContext(LoginContext)
  if (!context) throw new Error('Login hooks must be used within a LoginProvider')
  return context
}

/** Form chrome: submit handler and save state. Local validation runs first; the API error shows after. */
export function useLoginForm() {
  const { draft, saving, error, onLogin } = useLoginContext()
  return {
    saving,
    error,
    onSubmit: (event: FormEvent) => {
      event.preventDefault()
      const credentials = draft.submit()
      if (credentials) onLogin(credentials)
    },
  }
}

export function useEmailField() {
  const { draft } = useLoginContext()
  return { value: draft.email, setValue: draft.setEmail, problem: draft.emailProblem, showErrors: draft.showErrors }
}

export function usePasswordField() {
  const { draft } = useLoginContext()
  return {
    value: draft.password,
    setValue: draft.setPassword,
    problem: draft.passwordProblem,
    showErrors: draft.showErrors,
  }
}
