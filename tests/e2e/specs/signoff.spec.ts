import { type Browser, expect, type Page, test } from '@playwright/test'
import { demoPassword, demoUsers, signOffCandidates } from './demoUsers'

async function signedInPage(browser: Browser, email: string, baseURL: string | undefined, projectUse: object): Promise<Page> {
  const context = await browser.newContext({ ...projectUse, baseURL })
  const page = await context.newPage()
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL('/')
  return page
}

test('a candidate signs a skill off with a supervisor, the supervisor confirms, and the skill becomes current', async ({ browser, baseURL }, testInfo) => {
  const candidate = signOffCandidates[testInfo.project.name]!
  const { baseURL: _ignored, ...projectUse } = testInfo.project.use

  // Candidate: pick a skill that isn't certified and put it in the basket.
  const phone = await signedInPage(browser, candidate.email, baseURL, projectUse)
  await phone.goto('/skills?status=notCertified')
  const checkbox = phone.getByRole('checkbox', { name: /^Add .+ to basket$/ }).and(phone.locator(':enabled')).first()
  const code = (await checkbox.getAttribute('aria-label'))!.replace(/^Add (.+) to basket$/, '$1')
  await checkbox.check()
  await phone.getByRole('status').getByRole('button', { name: 'Review' }).click()

  // Basket → hand-off → reviewer mode.
  await expect(phone.getByRole('heading', { name: 'Basket (1)' })).toBeVisible()
  await phone.getByRole('button', { name: 'Sign off this one' }).click()
  await expect(phone.getByRole('heading', { name: 'Pass your phone to your reviewer' })).toBeVisible()
  await phone.getByRole('button', { name: "I'm the reviewer" }).click()
  await phone.getByLabel('Search reviewers').fill('Sofia')
  await phone.getByRole('button', { name: /Sofia Supervisor/ }).click()
  await expect(phone.getByRole('heading', { name: `Assess ${candidate.name}` })).toBeVisible()
  await phone.getByRole('button', { name: 'Continue to sign' }).click()

  // Sign on the pad and submit.
  const pad = phone.getByRole('img', { name: 'Signature pad' })
  const box = (await pad.boundingBox())!
  await phone.mouse.move(box.x + 20, box.y + box.height / 2)
  await phone.mouse.down()
  await phone.mouse.move(box.x + box.width / 2, box.y + 20, { steps: 8 })
  await phone.mouse.move(box.x + box.width - 20, box.y + box.height - 20, { steps: 8 })
  await phone.mouse.up()
  await phone.getByRole('button', { name: 'Submit & hand back' }).click()

  await expect(phone.getByRole('heading', { name: '1 review recorded' })).toBeVisible()
  await expect(phone.getByText(`Waiting for ${demoUsers.supervisor.name} to confirm.`)).toBeVisible()
  await phone.getByRole('button', { name: 'Back to basket' }).click()
  await expect(phone.getByRole('heading', { name: 'Basket (0)' })).toBeVisible()

  // Supervisor: confirm the sitting from the queue.
  const supervisor = await signedInPage(browser, demoUsers.supervisor.email, baseURL, projectUse)
  await supervisor.getByRole('button', { name: 'Review' }).click()
  const sitting = supervisor.getByRole('region', { name: new RegExp(`^${candidate.name},`) }).filter({ hasText: code }).first()
  await sitting.getByRole('button', { name: 'Confirm all' }).click()
  await expect(supervisor.getByRole('region', { name: new RegExp(`^${candidate.name},`) }).filter({ hasText: code })).toHaveCount(0)

  // Candidate: the skill is now current, signed by the supervisor.
  await phone.goto('/skills')
  await phone.getByLabel('Search skills').fill(code)
  await phone.getByRole('button', { name: new RegExp(`^${code.replaceAll('.', '\\.')}\\b`) }).first().click()
  await expect(phone).toHaveURL(/\/skills\/[0-9a-f-]{36}/)
  await expect(phone.getByRole('tab', { name: 'Overview' })).toBeVisible()
  await expect(phone.getByText('Current', { exact: true })).toBeVisible()
  await expect(phone.getByText(`${demoUsers.supervisor.name} · Supervisor`)).toBeVisible()
})
