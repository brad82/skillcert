/** Demo accounts seeded by the migrator (SkillCert.Infrastructure/Seed/DemoUsers.cs). */
export const demoPassword = process.env.E2E_PASSWORD ?? 'SkillCert-demo-2026'

export const demoUsers = {
  candidate: { email: 'candidate01@skillcert.test', name: 'Candidate 01' },
  admin: { email: 'admin@skillcert.test', name: 'Alex Admin' },
  // Used for the wrong-password check; the same test then signs in to reset the lockout counter.
  lockoutProbe: { email: 'candidate09@skillcert.test', name: 'Candidate 09' },
  // Has no seeded claims waiting, so the sign-off spec's queue holds only its own sittings.
  supervisor: { email: 'supervisor2@skillcert.test', name: 'Sofia Supervisor' },
} as const

/** One candidate per Playwright project: projects run in parallel against the same seeded data. */
export const signOffCandidates: Record<string, { email: string; name: string }> = {
  phone: { email: 'candidate06@skillcert.test', name: 'Candidate 06' },
  iphone: { email: 'candidate08@skillcert.test', name: 'Candidate 08' },
  desktop: { email: 'candidate10@skillcert.test', name: 'Candidate 10' },
}
