import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BasketProvider } from '@features/basket'
import type { PresentationState } from '@shared/lib/competencyStatus'
import { afaFragment } from '../../../test/fixtures'
import { TestProviders } from '../../../test/TestProviders'
import { SkillsProvider } from '../SkillsProvider'
import { SkillsPage } from './SkillsPage'

function renderSkills(status?: PresentationState) {
  const callbacks = { onClearStatus: vi.fn(), onOpenCompetency: vi.fn(), onOpenBasket: vi.fn() }
  render(
    <TestProviders>
      <BasketProvider userId={`test-${Math.random()}`}>
        <SkillsProvider lists={[afaFragment()]} status={status} {...callbacks}>
          <SkillsPage />
        </SkillsProvider>
      </BasketProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('SkillsPage', () => {
  it('shows the record as a tree, collapsing sections that are all current', async () => {
    renderSkills()

    expect(await screen.findByRole('heading', { name: 'AFA Skills Record' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /3 Management of injured or ill patients/ })).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByRole('button', { name: /4 Basic Life Support/ })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('One-rescuer adult CPR')).toBeInTheDocument()
    expect(screen.queryByText('Patient assessment')).not.toBeInTheDocument()
    expect(screen.getAllByLabelText('1 of 3 current')).toHaveLength(2) // section 4 and sub-section 4.3
  })

  it('narrows to what needs action and to a search', async () => {
    const { user } = renderSkills()

    await user.click(await screen.findByRole('button', { name: 'Needs action' }))
    expect(screen.getByText('One-rescuer adult CPR')).toBeInTheDocument()
    expect(screen.queryByText('Two-rescuer adult CPR')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'All' }))
    await user.type(screen.getByLabelText('Search skills'), 'ppe')
    expect(screen.getByText('Use of PPE')).toBeInTheDocument()
    expect(screen.queryByText('One-rescuer adult CPR')).not.toBeInTheDocument()
  })

  it('adds to the basket from the checkbox and offers to review it', async () => {
    const { user, onOpenBasket } = renderSkills()

    await user.click(await screen.findByLabelText('Add 4.3.1 to basket'))

    const bar = screen.getByRole('status')
    expect(within(bar).getByText('1 in basket')).toBeInTheDocument()
    await user.click(within(bar).getByRole('button', { name: 'Review' }))
    expect(onOpenBasket).toHaveBeenCalledOnce()
  })

  it('does not let a pending skill be added again', async () => {
    renderSkills()

    expect(await screen.findByLabelText('Add 4.3.3 to basket')).toBeDisabled()
  })

  it('applies the state chosen on Home and lets it be removed', async () => {
    const { user, onClearStatus } = renderSkills('expired')

    expect(await screen.findByText('One-rescuer adult CPR')).toBeInTheDocument()
    expect(screen.queryByText('Two-rescuer adult CPR')).not.toBeInTheDocument()
    await user.click(screen.getByLabelText('Remove filter: Expired'))
    expect(onClearStatus).toHaveBeenCalledOnce()
  })

  it('opens a competency when its row is tapped', async () => {
    const { user, onOpenCompetency } = renderSkills()

    await user.click(await screen.findByText('One-rescuer adult CPR'))

    expect(onOpenCompetency).toHaveBeenCalledOnce()
  })
})
