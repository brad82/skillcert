import type { CurrencyDto, MyCompetencyResponse, MyListDto, MyListNodeDto, MyReviewDto, ReviewLevelDto } from '@shared/api/model'

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

export const review = (overrides: Partial<MyReviewDto> = {}): MyReviewDto => ({
  id: id(),
  outcome: 'Competent',
  reviewedAt: '2026-06-01T12:00:00Z',
  reviewerName: 'Ines Instructor',
  method: 'Classified',
  classificationCode: 'Instructor',
  revisionNumber: 1,
  confirmationStatus: 'NotRequired',
  decidedAt: null,
  rejectionReason: null,
  comment: null,
  signatureId: null,
  signedWith: [],
  ...overrides,
})

/** One-rescuer adult CPR, current on revision 2 after a breaking revision invalidated the older sign-off. */
export function cprDetail(overrides: Partial<MyCompetencyResponse> = {}): MyCompetencyResponse {
  const latest = review({
    reviewedAt: '2026-06-01T12:00:00Z',
    revisionNumber: 2,
    signatureId: 'signature-1',
    comment: 'Good depth and rate.',
    signedWith: [{ competencyId: id(), code: '4.3.2', shortTitle: 'Two-rescuer adult CPR' }],
  })
  const older = review({ reviewedAt: '2025-03-01T12:00:00Z', reviewerName: 'Ivan Instructor' })
  return {
    asOf: '2026-10-04T12:00:00Z',
    competencyId: id(),
    code: '4.3.1',
    title: 'CPR – one-rescuer adult',
    shortTitle: 'One-rescuer adult CPR',
    description: 'Single-rescuer adult CPR at the correct rate and depth.',
    currency: currency(),
    lowestReviewer: instructorOrHigher,
    revision: { number: 2, publishedAt: '2026-05-01T12:00:00Z', recertificationDays: 365 },
    effectiveReview: latest,
    pendingReview: null,
    partOf: [{ listId: 'list-afa', listTitle: 'AFA Skills Record', headings: ['4 Basic Life Support', '4.3 CPR'] }],
    resources: [
      { title: 'CPR quick reference', url: 'https://example.org/cpr', type: 'WebPage' },
      { title: 'Adult CPR walkthrough', url: 'https://example.org/cpr-video', type: 'Video' },
    ],
    history: [
      { kind: 'Review', at: latest.reviewedAt, review: latest, revisionNumber: null },
      { kind: 'Invalidated', at: '2026-05-01T12:00:00Z', review: null, revisionNumber: 2 },
      { kind: 'Review', at: older.reviewedAt, review: older, revisionNumber: null },
    ],
    ...overrides,
  }
}
