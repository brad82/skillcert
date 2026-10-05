/** Demo accounts seeded by the migrator (SkillCert.Infrastructure/Seed/DemoUsers.cs). */
export const demoPassword = process.env.E2E_PASSWORD ?? 'SkillCert-demo-2026'

export const demoUsers = {
  candidate: { email: 'candidate01@skillcert.test', name: 'Candidate 01' },
  admin: { email: 'admin@skillcert.test', name: 'Alex Admin' },
  // Used for the wrong-password check; the same test then signs in to reset the lockout counter.
  lockoutProbe: { email: 'candidate09@skillcert.test', name: 'Candidate 09' },
} as const
