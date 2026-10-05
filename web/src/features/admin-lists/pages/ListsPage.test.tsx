import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AdminListSummaryDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { ListsProvider } from '../ListsProvider'
import { ListsPage } from './ListsPage'

const summary = (id: string, title: string): AdminListSummaryDto => ({ id, title, description: null, isActive: true, competencyCount: 3, groups: [] })

describe('ListsPage', () => {
  it('searches lists and creates a new one with a title', async () => {
    const onCreate = vi.fn().mockResolvedValue(true)
    render(
      <TestProviders>
        <TestRouter>
          <ListsProvider lists={[summary('a', 'AFA Skills Record'), summary('h', 'High angle rescue')]} saving={false} error={null} onCreate={onCreate}>
            <ListsPage />
          </ListsProvider>
        </TestRouter>
      </TestProviders>,
    )
    const user = userEvent.setup()

    const table = await screen.findByRole('table', { name: 'Competency lists' })
    await user.type(screen.getByRole('searchbox', { name: 'Search lists' }), 'rescue')
    expect(within(table).queryByText('AFA Skills Record')).not.toBeInTheDocument()
    expect(within(table).getByText('High angle rescue')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'New list' }))
    const dialog = screen.getByRole('dialog', { name: 'New list' })
    const submit = within(dialog).getByRole('button', { name: 'Create list' })
    expect(submit).toBeDisabled()
    await user.type(within(dialog).getByRole('textbox', { name: /Title/ }), '  Lift evacuation ')
    await user.click(submit)
    expect(onCreate).toHaveBeenCalledWith('Lift evacuation', null)
  })
})
