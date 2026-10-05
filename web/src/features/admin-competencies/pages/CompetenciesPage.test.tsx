import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { AdminCompetencyListItemDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { CompetenciesProvider } from '../CompetenciesProvider'
import { CompetenciesPage } from './CompetenciesPage'

const item = (id: string, code: string, title: string, listCount: number, recertificationDays: number | null): AdminCompetencyListItemDto => ({
  id,
  code,
  title,
  shortTitle: title,
  isActive: true,
  revisionNumber: 1,
  recertificationDays,
  lowestReviewer: { method: 'Peer', classificationCode: null, order: 1 },
  listCount,
})

describe('CompetenciesPage', () => {
  it('finds competencies that are in no list', async () => {
    render(
      <TestProviders>
        <TestRouter>
          <CompetenciesProvider competencies={[item('a', '4.3.1', 'One-rescuer adult CPR', 2, 730), item('b', '13.1', 'Avalanche transceiver search', 0, null)]}>
            <CompetenciesPage />
          </CompetenciesProvider>
        </TestRouter>
      </TestProviders>,
    )
    const user = userEvent.setup()

    const table = await screen.findByRole('table', { name: 'All competencies' })
    expect(within(table).getByText('Every 2 years')).toBeInTheDocument()
    expect(within(table).getByText('Never expires')).toBeInTheDocument()

    await user.click(screen.getByRole('combobox', { name: 'Show' }))
    await user.click(screen.getByRole('option', { name: 'Not in any list' }))
    expect(within(table).queryByText('One-rescuer adult CPR')).not.toBeInTheDocument()
    expect(within(table).getByText('Avalanche transceiver search')).toBeInTheDocument()
  })
})
