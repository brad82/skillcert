import { expect, test } from '@playwright/test'
import { demoPassword, demoUsers } from './demoUsers'

test('a candidate downloads their current training record as a PDF', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill(demoUsers.candidate.email)
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL('/')

  await page.getByRole('link', { name: 'Records' }).click()
  await expect(page.getByRole('heading', { name: 'Records', exact: true })).toBeVisible()
  await expect(page.getByText(/^Compliant/)).toBeVisible()

  const downloadPromise = page.waitForEvent('download')
  await page.getByRole('link', { name: 'Download current record (PDF)' }).click()
  const download = await downloadPromise

  expect(download.suggestedFilename()).toBe(`AFA Skills Record - ${demoUsers.candidate.name}.pdf`)
  const stream = await download.createReadStream()
  const chunks: Buffer[] = []
  for await (const chunk of stream) chunks.push(chunk as Buffer)
  expect(Buffer.concat(chunks).subarray(0, 4).toString()).toBe('%PDF')
})
