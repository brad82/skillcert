import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { ApprovalGroupDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { ApprovalsProvider } from '../ApprovalsProvider'
import { ApprovalsPage } from './ApprovalsPage'

const sitting: ApprovalGroupDto = {
  signatureId: 'sig-1',
  candidateUserId: 'u-c05',
  candidateName: 'Candidate 05',
  reviewedAt: '2026-10-02T15:00:00Z',
  comment: 'Good scene control.',
  items: [
    { reviewId: 'r1', competencyId: 'c1', code: '9.1a', shortTitle: 'Spinal immobilisation', outcome: 'Competent', revisionNumber: 1 },
    { reviewId: 'r2', competencyId: 'c2', code: '9.1b', shortTitle: 'Log roll', outcome: 'NotCompetent', revisionNumber: 1 },
  ],
}

function renderApprovals(groups: ApprovalGroupDto[] = [sitting]) {
  const callbacks = { onConfirm: vi.fn(), onReject: vi.fn() }
  render(
    <TestProviders>
      <ApprovalsProvider groups={groups} deciding={null} error={null} {...callbacks}>
        <ApprovalsPage />
      </ApprovalsProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('ApprovalsPage', () => {
  it('shows each sitting with its verdicts, comment and signature, and confirms it as a whole', async () => {
    const { user, onConfirm } = renderApprovals()

    const card = await screen.findByRole('region', { name: /Candidate 05/ })
    expect(within(card).getByText('Spinal immobilisation')).toBeInTheDocument()
    expect(within(card).getByText('Not competent')).toBeInTheDocument()
    expect(within(card).getByText('Good scene control.')).toBeInTheDocument()
    expect(within(card).getByRole('img')).toHaveAttribute('src', '/api/approvals/sig-1/signature')

    await user.click(within(card).getByRole('button', { name: 'Confirm all' }))
    expect(onConfirm).toHaveBeenCalledWith('sig-1')
  })

  it('rejects only with a reason', async () => {
    const { user, onReject } = renderApprovals()

    await user.click(await screen.findByRole('button', { name: 'Reject' }))
    const dialog = screen.getByRole('dialog', { name: "Reject Candidate 05's sign-off?" })
    const submit = within(dialog).getByRole('button', { name: 'Reject' })
    expect(submit).toBeDisabled()

    await user.type(within(dialog).getByLabelText(/Reason/), '  Not observed.  ')
    await user.click(submit)
    expect(onReject).toHaveBeenCalledWith('sig-1', 'Not observed.')
  })

  it('says when nothing is waiting', async () => {
    renderApprovals([])

    expect(await screen.findByText('Nothing is waiting for your confirmation.')).toBeInTheDocument()
  })
})
