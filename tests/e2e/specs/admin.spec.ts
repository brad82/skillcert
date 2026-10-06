import { expect, type Page, test } from '@playwright/test'
import { adminTargets, demoPassword, demoUsers } from './demoUsers'

async function signIn(page: Page, email: string) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL('/')
}

test('a non-administrator is sent back from the admin area', async ({ page }) => {
  await signIn(page, demoUsers.candidate.email)

  await page.goto('/admin/users')

  await expect(page).toHaveURL('/')
  await expect(page.getByRole('heading', { name: 'Hi Candidate' })).toBeVisible()
})

test('an administrator edits a user, builds a list and finds it in the audit log', async ({ page }, testInfo) => {
  const target = adminTargets[testInfo.project.name]!
  const tag = `${testInfo.project.name}-${Date.now().toString(36)}`

  // Into the admin area from the account menu.
  await signIn(page, demoUsers.admin.email)
  await page.getByRole('button', { name: 'Account' }).click()
  await page.getByRole('menuitem', { name: 'Administration' }).click()
  await expect(page).toHaveURL(/\/admin\/users/)

  // Users: give the target user the Instructor classification, then take it away again.
  await page.getByLabel('Search by name or email').fill(target.name)
  await page.getByRole('row', { name: new RegExp(target.name) }).getByText(target.name).click()
  const panel = page.getByRole('complementary', { name: target.name })
  const instructor = panel.getByRole('checkbox', { name: 'Instructor' })
  // Controlled by the saved state: each box flips once the API has answered. Start clean (re-runs, retries).
  if (await instructor.isChecked()) {
    await instructor.click()
    await page.getByRole('button', { name: 'Remove Instructor' }).click()
    await expect(instructor).not.toBeChecked()
  }
  await instructor.click()
  await expect(instructor).toBeChecked()
  await instructor.click()
  await page.getByRole('button', { name: 'Remove Instructor' }).click()
  await expect(instructor).not.toBeChecked()

  // Lists: a new list with a heading, an existing (shared) competency and a new one.
  await page.goto('/admin/lists')
  await page.getByRole('button', { name: 'New list' }).click()
  await page.getByLabel('Title').fill(`E2E ${tag}`)
  await page.getByRole('button', { name: 'Create list' }).click()
  await expect(page.getByRole('heading', { name: `E2E ${tag}` })).toBeVisible()

  await page.getByRole('button', { name: 'Add heading' }).click()
  await page.getByRole('dialog').getByLabel('Title').fill('Section')
  await page.getByRole('dialog').getByRole('button', { name: 'Add heading' }).click()
  await expect(page.getByRole('treeitem', { name: /Section/ })).toBeVisible()

  await page.getByRole('button', { name: 'Add existing competency' }).click()
  await page.getByLabel('Search all competencies').fill('3.2')
  await page.getByRole('checkbox', { name: /^3\.2 / }).click()
  await page.getByRole('button', { name: 'Add 1 competency' }).click()
  const shared = page.getByRole('treeitem', { name: /3\.2/ })
  await expect(shared).toBeVisible()
  await expect(shared.getByText(/In \d+ lists/)).toBeVisible()

  await page.getByRole('button', { name: 'New competency' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByRole('textbox', { name: /^Code/ }).fill(`E2E-${tag}`)
  await dialog.getByRole('textbox', { name: /^Title/ }).fill('End-to-end test skill')
  await dialog.getByRole('button', { name: 'Create and add to list' }).click()
  await expect(page.getByRole('treeitem', { name: new RegExp(`E2E-${tag}`, 'i') })).toBeVisible()

  // Audit: the list's history shows what just happened.
  await page.getByRole('link', { name: 'History' }).click()
  await expect(page).toHaveURL(/\/admin\/audit/)
  await expect(page.getByText('Created a list').first()).toBeVisible()
  await expect(page.getByText('Added competencies').first()).toBeVisible()
})
