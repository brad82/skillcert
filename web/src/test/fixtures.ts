import type { CurrencyDto, MyListDto, MyListNodeDto, ReviewLevelDto } from '@shared/api/model'

/** Builders for API-shaped test data. */
export const currency = (overrides: Partial<CurrencyDto> = {}): CurrencyDto => ({
  status: 'Current',
  reason: 'Current',
  achievedAt: '2026-06-01T12:00:00Z',
  expiresAt: '2027-06-01T12:00:00Z',
  expiringSoon: false,
  hasPendingReview: false,
  ...overrides,
})

export const instructorOrHigher: ReviewLevelDto = { method: 'Classified', classificationCode: 'Instructor', order: 11 }

let nextId = 1
const id = () => `00000000-0000-0000-0000-${String(nextId++).padStart(12, '0')}`

export function heading(code: string, title: string, parentNodeId: string | null, depth: number): MyListNodeDto {
  return { id: id(), parentNodeId, depth, kind: 'Heading', headingCode: code, headingTitle: title, competency: null }
}

export function leaf(code: string, title: string, parentNodeId: string | null, depth: number, state: Partial<CurrencyDto> = {}): MyListNodeDto {
  return {
    id: id(),
    parentNodeId,
    depth,
    kind: 'Competency',
    headingCode: null,
    headingTitle: null,
    competency: { competencyId: id(), code, title, shortTitle: title, currency: currency(state), lowestReviewer: instructorOrHigher },
  }
}

/** A small AFA-shaped list: one fully-current section and one needing action. */
export function afaFragment(): MyListDto {
  const patients = heading('3', 'Management of injured or ill patients', null, 0)
  const bls = heading('4', 'Basic Life Support', null, 0)
  const cpr = heading('4.3', 'CPR', bls.id, 1)
  const nodes = [
    patients,
    leaf('3.1', 'Patient assessment', patients.id, 1),
    leaf('3.2', 'Use of PPE', patients.id, 1),
    bls,
    cpr,
    leaf('4.3.1', 'One-rescuer adult CPR', cpr.id, 2, { status: 'Expired', reason: 'RecertificationExpired' }),
    leaf('4.3.2', 'Two-rescuer adult CPR', cpr.id, 2),
    leaf('4.3.3', 'One-rescuer child CPR', cpr.id, 2, { status: 'NotCertified', reason: 'NeverReviewed', achievedAt: null, expiresAt: null, hasPendingReview: true }),
  ]
  return {
    id: 'list-afa',
    title: 'AFA Skills Record',
    isCompliant: false,
    counts: { total: 5, current: 3, expiringSoon: 0, expired: 1, notCompetent: 0, notCertified: 1, pending: 1 },
    nodes,
  }
}
