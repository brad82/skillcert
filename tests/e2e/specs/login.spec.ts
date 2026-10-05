import { expect, type Page, test } from '@playwright/test'
import { demoPassword, demoUsers } from './demoUsers'

async function signIn(page: Page, email: string, password = demoPassword) {
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
}

test('a signed-out visitor is sent to sign in, and returned to where they were going', async ({ page }) => {
  await page.goto('/')

  await expect(page).toHaveURL(/\/login\?redirect=%2F/)
  await expect(page.getByRole('heading', { name: 'Sign in to SkillCert' })).toBeVisible()
})

test('the form explains what is missing before calling the API', async ({ page }) => {
  await page.goto('/login')

  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByText('Email is required')).toBeVisible()
  await expect(page.getByText('Password is required')).toBeVisible()
})

test('a wrong password is refused without saying which part was wrong', async ({ page }) => {
  await page.goto('/login')

  await signIn(page, demoUsers.lockoutProbe.email, 'not-the-password')
  await expect(page.getByRole('alert')).toContainText('Email or password is incorrect')

  // A successful sign-in resets the lockout counter so repeated runs never lock the account.
  await signIn(page, demoUsers.lockoutProbe.email)
  await expect(page.getByRole('heading', { name: 'Hi Candidate' })).toBeVisible()
})

test('a candidate signs in, lands on Home, and signs out from the account menu', async ({ page }) => {
  await page.goto('/login')

  await signIn(page, demoUsers.candidate.email)

  await expect(page).toHaveURL('/')
  await expect(page.getByRole('heading', { name: 'Hi Candidate' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'AFA Skills Record' })).toBeVisible()

  await page.getByRole('button', { name: 'Account' }).click()
  await expect(page.getByText(demoUsers.candidate.name)).toBeVisible()
  await page.getByRole('menuitem', { name: 'Sign out' }).click()
  await expect(page).toHaveURL(/\/login/)
})

test('an administrator sees the Administrator capability in the account menu', async ({ page }) => {
  await page.goto('/login')

  await signIn(page, demoUsers.admin.email)
  await page.getByRole('button', { name: 'Account' }).click()

  await expect(page.getByText('Administrator', { exact: true })).toBeVisible()
})

test('the sign-in page switches to French', async ({ page }) => {
  await page.goto('/login')

  await page.getByRole('button', { name: 'Change language' }).click()

  await expect(page.getByRole('heading', { name: 'Connexion à SkillCert' })).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('lang', 'fr')
})
