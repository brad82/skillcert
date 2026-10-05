import { fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BasketItem } from '@features/basket'
import type { SignOffResponse, SignOffReviewerDto } from '@shared/api/model'
import { instructorOrHigher } from '../../../test/fixtures'
import { TestProviders } from '../../../test/TestProviders'
import { SignOffProvider } from '../SignOffProvider'
import { SignOffPage } from './SignOffPage'

const items: BasketItem[] = [
  { competencyId: 'c-cpr', code: '4.3.1', title: 'One-rescuer adult CPR', lowestReviewer: instructorOrHigher },
  { competencyId: 'c-cpr2', code: '4.3.2', title: 'Two-rescuer adult CPR', lowestReviewer: instructorOrHigher },
]

const reviewers: SignOffReviewerDto[] = [
  { userId: 'u-ines', displayName: 'Ines Instructor', method: 'Classified', classificationCode: 'Instructor', signatureRequired: true, needsConfirmation: false },
  { userId: 'u-sam', displayName: 'Sam Supervisor', method: 'Classified', classificationCode: 'Supervisor', signatureRequired: true, needsConfirmation: true },
]

function renderSignOff(result: SignOffResponse | null = null) {
  const callbacks = { onSubmit: vi.fn(), onCancel: vi.fn(), onDone: vi.fn() }
  render(
    <TestProviders>
      <SignOffProvider items={items} candidateName="Candidate 04" reviewers={reviewers} submitting={false} error={null} result={result} {...callbacks}>
        <SignOffPage />
      </SignOffProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('SignOffPage', () => {
  beforeEach(() => {
    // jsdom has no canvas; the pad's drawing is visual only.
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(null)
  })

  it('hands off to the reviewer, records exceptions and a signature, and submits once', async () => {
    const { user, onSubmit } = renderSignOff()

    expect(await screen.findByRole('heading', { name: 'Pass your phone to your reviewer' })).toBeInTheDocument()
    expect(screen.getByText('Instructor or higher')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: "I'm the reviewer" }))

    expect(screen.getByText('Reviewer mode')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Search reviewers'), 'sam')
    expect(screen.queryByText('Ines Instructor')).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Sam Supervisor/ }))

    expect(screen.getByRole('heading', { name: 'Assess Candidate 04' })).toBeInTheDocument()
    const cpr2 = screen.getByRole('group', { name: 'Outcome for 4.3.2' })
    await user.click(within(cpr2).getByRole('button', { name: 'Not competent' }))
    await user.type(screen.getByLabelText('Comment (optional)'), 'Recheck ventilations.')
    await user.click(screen.getByRole('button', { name: 'Continue to sign' }))

    expect(screen.getByText(/Sam Supervisor \(Supervisor\) confirms 1 Competent, 1 Not competent for Candidate 04/)).toBeInTheDocument()
    const submit = screen.getByRole('button', { name: 'Submit & hand back' })
    expect(submit).toBeDisabled()

    const pad = screen.getByRole('img', { name: 'Signature pad' })
    fireEvent.pointerDown(pad, { clientX: 10, clientY: 20, pointerId: 1 })
    fireEvent.pointerMove(pad, { clientX: 30, clientY: 40, pointerId: 1 })
    fireEvent.pointerUp(pad, { pointerId: 1 })
    await user.click(submit)

    expect(onSubmit).toHaveBeenCalledOnce()
    const request = onSubmit.mock.calls[0]![0]
    expect(request).toMatchObject({
      reviewerUserId: 'u-sam',
      items: [
        { competencyId: 'c-cpr', outcome: 'Competent' },
        { competencyId: 'c-cpr2', outcome: 'NotCompetent' },
      ],
      comment: 'Recheck ventilations.',
    })
    expect(request.signature.strokes).toHaveLength(1)
  })

  it('EXIT hands the phone back and forgets what the reviewer entered', async () => {
    const { user } = renderSignOff()

    await user.click(await screen.findByRole('button', { name: "I'm the reviewer" }))
    await user.click(screen.getByRole('button', { name: /Ines Instructor/ }))
    await user.click(screen.getByRole('button', { name: 'Exit' }))

    expect(screen.getByRole('heading', { name: 'Pass your phone to your reviewer' })).toBeInTheDocument()
    expect(screen.queryByText('Reviewer mode')).not.toBeInTheDocument()
  })

  it('shows the result, with who still has to confirm', async () => {
    const { user, onDone } = renderSignOff({
      reviewedAt: '2026-10-04T12:00:00Z',
      reviewerName: 'Sam Supervisor',
      method: 'Classified',
      classificationCode: 'Supervisor',
      reviews: [
        { reviewId: 'r1', competencyId: 'c-cpr', code: '4.3.1', shortTitle: 'One-rescuer adult CPR', outcome: 'Competent', confirmationStatus: 'Pending' },
        { reviewId: 'r2', competencyId: 'c-cpr2', code: '4.3.2', shortTitle: 'Two-rescuer adult CPR', outcome: 'NotCompetent', confirmationStatus: 'Pending' },
      ],
    })

    expect(await screen.findByRole('heading', { name: '2 reviews recorded' })).toBeInTheDocument()
    expect(screen.getByText('Waiting for Sam Supervisor to confirm.')).toBeInTheDocument()
    expect(screen.getAllByText('Pending')).toHaveLength(2)
    await user.click(screen.getByRole('button', { name: 'Back to basket' }))
    expect(onDone).toHaveBeenCalledOnce()
  })
})
