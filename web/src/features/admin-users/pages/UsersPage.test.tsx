import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AdminUserDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { UsersProvider } from '../UsersProvider'
import { UsersPage } from './UsersPage'

const user = (overrides: Partial<AdminUserDto>): AdminUserDto => ({
  id: 'u-x',
  displayName: 'X',
  email: 'x@example.org',
  isActive: true,
  isAdministrator: false,
  classifications: [],
  groups: [],
  createdAt: '2026-09-01T12:00:00Z',
  ...overrides,
})

const users = [
  user({ id: 'u-admin', displayName: 'Alex Admin', email: 'alex@example.org', isAdministrator: true }),
  user({ id: 'u-ines', displayName: 'Ines Instructor', email: 'ines@example.org', classifications: ['Instructor'] }),
  user({ id: 'u-morgan', displayName: 'Morgan Moreau', email: 'morgan@example.org', isActive: false }),
]

function renderUsers() {
  const callbacks = { onSetActive: vi.fn(), onSetClassification: vi.fn() }
  render(
    <TestProviders>
      <TestRouter>
        <UsersProvider
          users={users}
          classifications={[
            { code: 'Supervisor', name: 'Supervisor', rank: 2 },
            { code: 'Instructor', name: 'Instructor', rank: 1 },
          ]}
          currentUserId="u-admin"
          busy={false}
          error={null}
          {...callbacks}
        >
          <UsersPage />
        </UsersProvider>
      </TestRouter>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('UsersPage', () => {
  it('filters by name or email and never offers to deactivate yourself', async () => {
    const { user } = renderUsers()

    const table = await screen.findByRole('table', { name: 'Users' })
    const self = within(table).getByRole('row', { name: /Alex Admin/ })
    expect(within(self).getByText('This is you')).toBeInTheDocument()
    expect(within(self).queryByRole('button', { name: 'Deactivate' })).not.toBeInTheDocument()

    await user.type(screen.getByLabelText('Search by name or email'), 'ines@')
    expect(within(table).queryByText('Morgan Moreau')).not.toBeInTheDocument()
    expect(within(table).getByText('Ines Instructor')).toBeInTheDocument()
  })

  it('asks before deactivating, and reactivates without asking', async () => {
    const { user, onSetActive } = renderUsers()

    const ines = within(await screen.findByRole('table', { name: 'Users' })).getByRole('row', { name: /Ines Instructor/ })
    await user.click(within(ines).getByRole('button', { name: 'Deactivate' }))
    const dialog = screen.getByRole('alertdialog', { name: 'Deactivate Ines Instructor?' })
    expect(onSetActive).not.toHaveBeenCalled()
    await user.click(within(dialog).getByRole('button', { name: 'Deactivate Ines' }))
    expect(onSetActive).toHaveBeenCalledWith('u-ines', false)

    const morgan = await screen.findByRole('row', { name: /Morgan Moreau/ })
    await user.click(within(morgan).getByRole('button', { name: 'Reactivate' }))
    expect(onSetActive).toHaveBeenCalledWith('u-morgan', true)
  })

  it('assigns a classification straight away but asks before removing one', async () => {
    const { user, onSetClassification } = renderUsers()

    await user.click(await screen.findByRole('button', { name: 'Ines Instructor' }))
    const panel = screen.getByRole('complementary', { name: 'Ines Instructor' })

    await user.click(within(panel).getByRole('checkbox', { name: 'Supervisor' }))
    expect(onSetClassification).toHaveBeenCalledWith('u-ines', 'Supervisor', true)

    await user.click(within(panel).getByRole('checkbox', { name: 'Instructor' }))
    const dialog = screen.getByRole('alertdialog', { name: 'Remove Instructor from Ines Instructor?' })
    expect(within(dialog).getByText('Ines can still confirm or reject claims that already name them.')).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: 'Remove Instructor' }))
    expect(onSetClassification).toHaveBeenCalledWith('u-ines', 'Instructor', false)
  })
})
