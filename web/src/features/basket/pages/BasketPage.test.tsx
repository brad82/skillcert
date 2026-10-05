import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useEffect, useRef } from 'react'
import { describe, expect, it, vi } from 'vitest'
import type { ReviewLevelDto } from '@shared/api/model'
import { instructorOrHigher } from '../../../test/fixtures'
import { TestProviders } from '../../../test/TestProviders'
import { BasketProvider, useBasket } from '../BasketProvider'
import type { BasketItem } from '../model/basketItem'
import { BasketContainer } from './BasketContainer'

const peerOrHigher: ReviewLevelDto = { method: 'Peer', classificationCode: null, order: 1 }
const supervisorOrHigher: ReviewLevelDto = { method: 'Classified', classificationCode: 'Supervisor', order: 21 }

const items: BasketItem[] = [
  { competencyId: 'c-cpr', code: '4.3.1', title: 'One-rescuer adult CPR', lowestReviewer: instructorOrHigher },
  { competencyId: 'c-ppe', code: '3.2', title: 'Use of PPE', lowestReviewer: peerOrHigher },
  { competencyId: 'c-aed', code: '4.4.1', title: 'AED', lowestReviewer: instructorOrHigher },
  { competencyId: 'c-spine', code: '9.1a', title: 'Spinal immobilisation', lowestReviewer: supervisorOrHigher },
]

function Seed({ with: seed }: { with: BasketItem[] }) {
  const basket = useBasket()
  const seeded = useRef(false)
  useEffect(() => {
    if (seeded.current) return
    seeded.current = true
    seed.forEach(basket.add)
  }, [basket, seed])
  return null
}

function renderBasket(seed = items) {
  const callbacks = { onSignOff: vi.fn(), onOpenSkills: vi.fn() }
  render(
    <TestProviders>
      <BasketProvider userId={`test-${Math.random()}`}>
        <Seed with={seed} />
        <BasketContainer {...callbacks} />
      </BasketProvider>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('BasketPage', () => {
  it('groups by the lowest reviewer, lowest first, and signs off one group', async () => {
    const { user, onSignOff } = renderBasket()

    expect(await screen.findByRole('heading', { name: 'Basket (4)' })).toBeInTheDocument()
    expect(screen.getAllByRole('region').map((r) => r.getAttribute('aria-label'))).toEqual([
      'Peer or higher',
      'Instructor or higher',
      'Supervisor or higher',
    ])

    await user.click(within(screen.getByRole('region', { name: 'Instructor or higher' })).getByRole('button', { name: 'Sign off these 2' }))
    expect(onSignOff).toHaveBeenCalledWith(['c-cpr', 'c-aed'])
  })

  it('removes items, merges everything under the highest level, and clears', async () => {
    const { user, onSignOff } = renderBasket()

    await user.click(await screen.findByRole('button', { name: 'Remove 9.1a' }))
    expect(screen.getByText('Any Instructor could sign all 3 at once.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Merge' }))
    const merged = screen.getByRole('region', { name: 'Instructor or higher' })
    await user.click(within(merged).getByRole('button', { name: 'Sign off these 3' }))
    expect(onSignOff).toHaveBeenCalledWith(['c-ppe', 'c-cpr', 'c-aed'])

    await user.click(screen.getByRole('button', { name: 'Clear' }))
    expect(screen.getByRole('heading', { name: 'Basket (0)' })).toBeInTheDocument()
    expect(screen.getByText(/Your basket is empty/)).toBeInTheDocument()
  })
})
