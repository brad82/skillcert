import { defineConfig, devices } from '@playwright/test'

/**
 * Smoke tests against a running SkillCert stack. They never start it:
 * - locally, run the Aspire AppHost and set E2E_BASE_URL to the web resource URL;
 * - in CI, they run against the docker compose stack (http://localhost:8080).
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'https://localhost:8443',
    // The compose stack uses Caddy's local CA for localhost; the demo has a real certificate.
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    // Candidate flows are designed for phones first (development plan §1.4).
    { name: 'phone', use: { ...devices['Pixel 7'] } },
    // Safari is stricter about cookies on http://localhost than Chromium; keep it covered.
    { name: 'iphone', use: { ...devices['iPhone 15'] } },
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
  ],
})
