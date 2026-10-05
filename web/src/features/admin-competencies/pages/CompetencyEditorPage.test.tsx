import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AdminCompetencyDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { CompetencyEditorProvider } from '../CompetencyEditorProvider'
import { CompetencyEditorPage } from './CompetencyEditorPage'

const competency: AdminCompetencyDto = {
  id: 'c431',
  code: '4.3.1',
  isActive: true,
  current: {
    number: 3,
    title: 'One-rescuer adult CPR',
    shortTitle: null,
    description: 'Check for response, then give chest compresions and rescue breaths.',
    recertificationDays: 365,
    lowestReviewer: { method: 'Classified', classificationCode: 'Instructor', order: 2 },
    resources: [],
    publishedAt: '2026-03-14T12:00:00Z',
    invalidatesPreviousReviews: true,
  },
  revisions: [{ number: 3, title: 'One-rescuer adult CPR', publishedAt: '2026-03-14T12:00:00Z', invalidatesPreviousReviews: true, recertificationDays: 365 }],
  lists: [
    { id: 'afa', title: 'AFA Skills Record' },
    { id: 'ref', title: 'Returning candidate refresher' },
  ],
}

function renderEditor() {
  const callbacks = {
    onSaveEdit: vi.fn().mockResolvedValue(true),
    onPublish: vi.fn().mockResolvedValue(true),
    onSetActive: vi.fn().mockResolvedValue(true),
  }
  render(
    <TestProviders>
      <TestRouter>
        <CompetencyEditorProvider competency={competency} fromListId="afa" busy={false} error={null} notice={null} {...callbacks}>
          <CompetencyEditorPage />
        </CompetencyEditorProvider>
      </TestRouter>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('CompetencyEditorPage', () => {
  it('recommends an edit for a one-field fix and saves it in place', async () => {
    const { user, onSaveEdit, onPublish } = renderEditor()

    const description = await screen.findByRole('textbox', { name: 'Description' })
    await user.clear(description)
    await user.type(description, 'Check for response, then give chest compressions and rescue breaths.')
    await user.click(screen.getByRole('button', { name: 'Publish changes…' }))

    const review = screen.getByRole('dialog', { name: /Review changes to 4.3.1/ })
    expect(within(review).getByText('You changed 1 item')).toBeInTheDocument()
    expect(within(review).getByText(/shared by 2 lists/)).toBeInTheDocument()
    expect(within(review).getByRole('radio', { name: /Save as an edit/ })).toBeChecked()
    await user.click(within(review).getByRole('button', { name: 'Save edit' }))

    expect(onSaveEdit).toHaveBeenCalledWith(expect.objectContaining({ description: 'Check for response, then give chest compressions and rescue breaths.' }))
    expect(onPublish).not.toHaveBeenCalled()
  })

  it('recommends a new revision for several changes, and asks twice before reassessing everyone', async () => {
    const { user, onPublish } = renderEditor()

    await user.type(await screen.findByRole('textbox', { name: 'Short title' }), 'Adult CPR, one rescuer')
    await user.click(screen.getByRole('tab', { name: 'Resources' }))
    await user.click(screen.getByRole('button', { name: 'Add link' }))
    await user.type(screen.getByRole('textbox', { name: 'Link 1 title' }), 'Pocket mask ventilation')
    await user.type(screen.getByRole('textbox', { name: 'Link 1 address' }), 'https://example.org/pocket-mask')
    await user.click(screen.getByRole('button', { name: 'Publish changes…' }))

    const review = screen.getByRole('dialog', { name: /Review changes/ })
    expect(within(review).getByText('You changed 2 items')).toBeInTheDocument()
    expect(within(review).getByRole('radio', { name: /Publish a new revision/ })).toBeChecked()
    await user.click(within(review).getByRole('button', { name: 'Continue' }))

    const signoffs = screen.getByRole('dialog', { name: /Publish a new revision of 4.3.1/ })
    const publish = within(signoffs).getByRole('button', { name: 'Publish new revision' })
    expect(publish).toBeDisabled()
    await user.click(within(signoffs).getByRole('radio', { name: /Require reassessment now/ }))
    await user.click(publish)

    const confirm = screen.getByRole('alertdialog', { name: 'Publish a new revision and require reassessment?' })
    const go = within(confirm).getByRole('button', { name: 'Publish and require reassessment' })
    expect(go).toBeDisabled()
    await user.click(within(confirm).getByRole('checkbox', { name: /I understand/ }))
    await user.click(go)

    expect(onPublish).toHaveBeenCalledWith(
      expect.objectContaining({ shortTitle: 'Adult CPR, one rescuer', resources: [{ title: 'Pocket mask ventilation', url: 'https://example.org/pocket-mask', type: 'WebPage' }] }),
      true,
    )
  })

  it('only offers a new revision when certification changed', async () => {
    const { user } = renderEditor()

    await user.click(await screen.findByRole('tab', { name: 'Certification' }))
    await user.click(screen.getByRole('radio', { name: 'Supervisor only' }))
    await user.click(screen.getByRole('button', { name: 'Publish changes…' }))

    const review = screen.getByRole('dialog', { name: /Review changes/ })
    expect(within(review).getByRole('radio', { name: /Save as an edit/ })).toBeDisabled()
    expect(within(review).getByRole('radio', { name: /Publish a new revision/ })).toBeChecked()
  })

  it('asks before deactivating', async () => {
    const { user, onSetActive } = renderEditor()

    await user.click(await screen.findByRole('button', { name: 'Deactivate' }))
    const dialog = screen.getByRole('alertdialog', { name: 'Deactivate 4.3.1 One-rescuer adult CPR?' })
    await user.click(within(dialog).getByRole('button', { name: 'Deactivate 4.3.1' }))
    expect(onSetActive).toHaveBeenCalledWith(false)
  })
})
