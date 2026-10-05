import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BasketProvider } from '@features/basket'
import type { MyCompetencyResponse } from '@shared/api/model'
import { cprDetail, currency, review } from '../../../test/fixtures'
import { TestProviders } from '../../../test/TestProviders'
import { CompetencyProvider } from '../CompetencyProvider'
import type { CompetencyTab } from '../model/competencyTabs'
import { CompetencyPage } from './CompetencyPage'

function renderDetail(competency: MyCompetencyResponse = cprDetail(), tab: CompetencyTab = 'overview') {
  const callbacks = { onTabChange: vi.fn(), onBack: vi.fn() }
  render(
    <TestProviders>
      <BasketProvider userId={`test-${Math.random()}`}>
        <CompetencyProvider competency={competency} tab={tab} {...callbacks}>
          <CompetencyPage />
        </CompetencyProvider>
      </BasketProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('CompetencyPage', () => {
  it('shows the overview and adds a current skill to the basket for reassessment', async () => {
    const { user, onBack } = renderDetail()

    expect(await screen.findByRole('heading', { name: 'One-rescuer adult CPR' })).toBeInTheDocument()
    expect(screen.getByText('Ines Instructor · Instructor')).toBeInTheDocument()
    expect(screen.getByText('Every 1 year')).toBeInTheDocument()
    expect(screen.getByText(/Instructor or higher/)).toBeInTheDocument()
    expect(screen.getByText('AFA Skills Record › 4 Basic Life Support › 4.3 CPR')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add to basket for reassessment' }))
    expect(screen.getByRole('button', { name: 'Remove from basket' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Back to skills' }))
    expect(onBack).toHaveBeenCalledOnce()
  })

  it('explains a pending claim and blocks adding it again', async () => {
    const pending = review({ reviewerName: 'Sam Supervisor', classificationCode: 'Supervisor', confirmationStatus: 'Pending' })
    renderDetail(
      cprDetail({
        currency: currency({ status: 'NotCertified', reason: 'NeverReviewed', achievedAt: null, expiresAt: null, hasPendingReview: true }),
        effectiveReview: null,
        pendingReview: pending,
      }),
    )

    expect(await screen.findByRole('note')).toHaveTextContent('Becomes Current once Sam Supervisor confirms.')
    expect(screen.getByRole('button', { name: 'Waiting for confirmation' })).toBeDisabled()
  })

  it('switches tabs through the route and lists resources grouped by type, opening externally', async () => {
    const { user, onTabChange } = renderDetail(cprDetail(), 'resources')

    const links = await screen.findAllByRole('link')
    expect(links.map((link) => link.textContent)).toEqual(['Adult CPR walkthrough', 'CPR quick reference'])
    expect(links[0]).toHaveAttribute('target', '_blank')
    expect(within(screen.getByRole('region', { name: 'Video' })).getByRole('link')).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'History (3)' }))
    expect(onTabChange).toHaveBeenCalledWith('history')
  })

  it('shows history with the breaking revision and opens a review with its sitting', async () => {
    const { user } = renderDetail(cprDetail(), 'history')

    expect(await screen.findByText('Revision 2 published')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Ines Instructor/ }))

    const sheet = await screen.findByRole('dialog', { name: 'Review detail' })
    expect(within(sheet).getByText('Good depth and rate.')).toBeInTheDocument()
    expect(within(sheet).getByAltText('Signature of Ines Instructor')).toHaveAttribute('src', '/api/me/signatures/signature-1')
    expect(within(sheet).getByText('Two-rescuer adult CPR')).toBeInTheDocument()
  })

  it('renders in French', async () => {
    render(
      <TestProviders locale="fr">
        <BasketProvider userId="test-fr">
          <CompetencyProvider competency={cprDetail()} tab="overview" onTabChange={vi.fn()} onBack={vi.fn()}>
            <CompetencyPage />
          </CompetencyProvider>
        </BasketProvider>
      </TestProviders>,
    )

    expect(await screen.findByRole('button', { name: 'Ajouter au panier pour réévaluation' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Aperçu' })).toBeInTheDocument()
  })
})
