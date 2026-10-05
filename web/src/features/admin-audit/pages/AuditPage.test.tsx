import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AuditEntryDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { AuditProvider } from '../AuditProvider'
import { AuditPage } from './AuditPage'

const entry: AuditEntryDto = {
  id: 'a1',
  at: '2026-10-04T14:32:00Z',
  actorUserId: 'u-admin',
  actorName: 'Alex Admin',
  action: 'competency.publish-revision',
  entityType: 'Competency',
  entityId: 'c431',
  entityLabel: '4.3.1',
  before: { revisionNumber: 3, recertificationDays: 365 },
  after: { revisionNumber: 4, recertificationDays: 730, invalidatesPreviousReviews: true },
}

function renderAudit(filters = {}) {
  const callbacks = { onLoadMore: vi.fn(), onFiltersChange: vi.fn() }
  render(
    <TestProviders>
      <AuditProvider entries={[entry]} entityTypes={['Competency', 'User']} actors={[{ id: 'u-admin', name: 'Alex Admin' }]} filters={filters} hasMore loadingMore={false} {...callbacks}>
        <AuditPage />
      </AuditProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('AuditPage', () => {
  it('names each action in plain words and expands to the values before and after', async () => {
    const { user, onLoadMore } = renderAudit()

    const table = await screen.findByRole('table', { name: 'Audit log' })
    expect(within(table).getByText('Published a new revision')).toBeInTheDocument()
    await user.click(within(table).getByRole('button', { name: 'Show details' }))
    const changes = within(table).getByRole('table', { name: 'Changes' })
    const days = within(changes).getByRole('row', { name: /recertificationDays/ })
    expect(within(days).getByText('365')).toBeInTheDocument()
    expect(within(days).getByText('730')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Load older entries' }))
    expect(onLoadMore).toHaveBeenCalled()
  })

  it('shows the one item it was opened for, and clears it', async () => {
    const { user, onFiltersChange } = renderAudit({ entityId: 'c431' })

    const chip = await screen.findByRole('button', { name: 'Only: 4.3.1' })
    await user.click(within(chip).getByTestId('CancelIcon'))
    expect(onFiltersChange).toHaveBeenCalledWith({ entityId: undefined })
  })
})
