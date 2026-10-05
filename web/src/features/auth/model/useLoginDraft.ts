import { useState } from 'react'
import { z } from 'zod'
import { LoginBody } from '@shared/schemas/skillcert.zod.gen'

export type Credentials = z.infer<typeof LoginBody>

/** Which check failed. Components translate these; Zod's own messages are never shown. */
export type FieldProblem = 'required' | 'invalid' | null

// The generated schema carries only the API's shape; the client adds the presence/format rules.
const credentialsSchema = LoginBody.extend({
  email: z.string().trim().min(1).pipe(z.email()),
  password: z.string().min(1),
})

export type LoginDraft = {
  email: string
  password: string
  setEmail: (value: string) => void
  setPassword: (value: string) => void
  emailProblem: FieldProblem
  passwordProblem: FieldProblem
  /** Set once the user tries to submit, so fields don't shout before they're touched. */
  showErrors: boolean
  /** Validates the whole draft. Returns trimmed credentials, or null and reveals field errors. */
  submit: () => Credentials | null
}

/**
 * Owns the sign-in form's field values and their validation.
 * Knows nothing about the API call, error copy, or where to go after sign-in — see LoginContainer.
 */
export function useLoginDraft(): LoginDraft {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showErrors, setShowErrors] = useState(false)

  const emailProblem: FieldProblem = !email.trim()
    ? 'required'
    : credentialsSchema.shape.email.safeParse(email).success
      ? null
      : 'invalid'
  const passwordProblem: FieldProblem = password ? null : 'required'

  function submit(): Credentials | null {
    setShowErrors(true)
    const result = credentialsSchema.safeParse({ email, password })
    return result.success ? result.data : null
  }

  return { email, password, setEmail, setPassword, emailProblem, passwordProblem, showErrors, submit }
}
