import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Locale } from '@shared/i18n/runtime'
import { TestProviders } from '../../../test/TestProviders'
import { LoginProvider } from '../LoginProvider'
import { LoginPage } from './LoginPage'

function renderLogin({ error = null, locale = 'en' }: { error?: string | null; locale?: Locale } = {}) {
  const onLogin = vi.fn()
  render(
    <TestProviders locale={locale}>
      <LoginProvider saving={false} error={error} onLogin={onLogin}>
        <LoginPage />
      </LoginProvider>
    </TestProviders>,
  )
  return { onLogin, user: userEvent.setup() }
}

describe('LoginPage', () => {
  it('shows required-field errors only after a submit attempt, and does not sign in', async () => {
    const { onLogin, user } = renderLogin()
    const signIn = await screen.findByRole('button', { name: 'Sign in' })

    expect(screen.queryByText('Email is required')).not.toBeInTheDocument()
    await user.click(signIn)

    expect(screen.getByText('Email is required')).toBeInTheDocument()
    expect(screen.getByText('Password is required')).toBeInTheDocument()
    expect(onLogin).not.toHaveBeenCalled()
  })

  it('rejects a malformed email', async () => {
    const { onLogin, user } = renderLogin()

    await user.type(await screen.findByLabelText(/Email/), 'not-an-email')
    await user.type(screen.getByLabelText(/Password/), 'SkillCert-demo-2026')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(screen.getByText('Enter a valid email address')).toBeInTheDocument()
    expect(onLogin).not.toHaveBeenCalled()
  })

  it('signs in with trimmed credentials', async () => {
    const { onLogin, user } = renderLogin()

    await user.type(await screen.findByLabelText(/Email/), '  candidate01@skillcert.test ')
    await user.type(screen.getByLabelText(/Password/), 'SkillCert-demo-2026')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(onLogin).toHaveBeenCalledExactlyOnceWith({
      email: 'candidate01@skillcert.test',
      password: 'SkillCert-demo-2026',
    })
  })

  it('shows the error the container passes down', async () => {
    renderLogin({ error: 'Email or password is incorrect.' })

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is incorrect.')
  })

  it('renders in French', async () => {
    renderLogin({ locale: 'fr' })

    expect(await screen.findByRole('heading', { name: 'Connexion à SkillCert' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Se connecter' })).toBeInTheDocument()
  })
})
