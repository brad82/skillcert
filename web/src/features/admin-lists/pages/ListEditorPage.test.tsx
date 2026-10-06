import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AdminCompetencyListItemDto, AdminListDto, AdminListNodeDto } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { ListEditorProvider } from '../ListEditorProvider'
import { ListEditorPage } from './ListEditorPage'

const heading = (id: string, parentNodeId: string | null, depth: number, index: number, code: string, title: string): AdminListNodeDto => ({
  id,
  parentNodeId,
  depth,
  index,
  kind: 'Heading',
  headingCode: code,
  headingTitle: title,
  competency: null,
})

const item = (id: string, parentNodeId: string, depth: number, index: number, competencyId: string, code: string, title: string): AdminListNodeDto => ({
  id,
  parentNodeId,
  depth,
  index,
  kind: 'Competency',
  headingCode: null,
  headingTitle: null,
  competency: { id: competencyId, code, title, shortTitle: title, isActive: true, listCount: 1 },
})

const list: AdminListDto = {
  id: 'list-afa',
  title: 'AFA Skills Record',
  description: null,
  isActive: true,
  groups: ['All patrollers'],
  nodes: [
    heading('n4', null, 0, 0, '4', 'Basic Life Support'),
    heading('n43', 'n4', 1, 0, '4.3', 'Cardiopulmonary resuscitation (CPR)'),
    item('n431', 'n43', 2, 0, 'c431', '4.3.1', 'One-rescuer adult CPR'),
    item('n432', 'n43', 2, 1, 'c432', '4.3.2', 'Two-rescuer adult CPR'),
  ],
}

const libraryItem = (id: string, code: string, title: string, listCount: number): AdminCompetencyListItemDto => ({
  id,
  code,
  title,
  shortTitle: title,
  isActive: true,
  revisionNumber: 1,
  recertificationDays: 365,
  lowestReviewer: { method: 'Classified', classificationCode: 'Instructor', order: 2 },
  listCount,
})

const library = [libraryItem('c431', '4.3.1', 'One-rescuer adult CPR', 2), libraryItem('c131', '13.1', 'Avalanche transceiver search', 0)]

function renderEditor(overrides: { onCreateCompetency?: () => Promise<'ok' | 'duplicate' | 'failed'> } = {}) {
  const ok = () => vi.fn().mockResolvedValue(true)
  const callbacks = {
    onRenameList: ok(),
    onAddHeading: ok(),
    onRenameHeading: ok(),
    onMove: ok(),
    onRemove: ok(),
    onAddCompetencies: ok(),
    onCreateCompetency: vi.fn(overrides.onCreateCompetency ?? (() => Promise.resolve('ok' as const))),
  }
  render(
    <TestProviders>
      <TestRouter>
        <ListEditorProvider list={list} library={library} busy={false} error={null} {...callbacks}>
          <ListEditorPage />
        </ListEditorProvider>
      </TestRouter>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('ListEditorPage', () => {
  it('collapses a heading and asks before removing it with everything under it', async () => {
    const { user, onRemove } = renderEditor()

    const tree = await screen.findByRole('tree', { name: 'List contents' })
    await user.click(within(tree).getByRole('button', { name: 'Collapse 4.3 Cardiopulmonary resuscitation (CPR)' }))
    expect(within(tree).queryByText('One-rescuer adult CPR')).not.toBeInTheDocument()
    expect(within(tree).getByText(/2 competencies/)).toBeInTheDocument()

    await user.click(within(tree).getByRole('button', { name: 'Actions for 4.3 Cardiopulmonary resuscitation (CPR)' }))
    await user.click(screen.getByRole('menuitem', { name: 'Remove from list' }))
    const dialog = screen.getByRole('alertdialog', { name: 'Remove 4.3 and everything under it?' })
    expect(onRemove).not.toHaveBeenCalled()
    await user.click(within(dialog).getByRole('button', { name: 'Remove 4.3 and 2 competencies' }))
    expect(onRemove).toHaveBeenCalledWith('n43')
  })

  it('moves a competency down among its siblings', async () => {
    const { user, onMove } = renderEditor()

    await user.click(await screen.findByRole('button', { name: 'Actions for 4.3.1 One-rescuer adult CPR' }))
    await user.click(screen.getByRole('menuitem', { name: 'Move down' }))
    expect(onMove).toHaveBeenCalledWith('n431', 'n43', 1)
  })

  it('offers to share an existing competency when a new one reuses its code', async () => {
    const { user, onCreateCompetency, onAddCompetencies } = renderEditor({ onCreateCompetency: () => Promise.resolve('duplicate') })

    await user.click(await screen.findByRole('button', { name: 'New competency' }))
    const create = screen.getByRole('dialog', { name: /New competency in AFA Skills Record/ })
    await user.type(within(create).getByRole('textbox', { name: 'Code' }), '13.1')
    await user.type(within(create).getByRole('textbox', { name: 'Title' }), 'Transceiver search')
    await user.click(within(create).getByRole('button', { name: 'Create and add to list' }))
    expect(onCreateCompetency).toHaveBeenCalledWith(null, expect.objectContaining({ code: '13.1' }))

    await user.click(await within(create).findByRole('button', { name: 'Add existing' }))
    const add = await screen.findByRole('dialog', { name: /Add existing competencies/ })
    expect(within(add).getByRole('searchbox', { name: 'Search all competencies' })).toHaveValue('13.1')
    await user.click(within(add).getByRole('checkbox', { name: '13.1 Avalanche transceiver search' }))
    await user.click(within(add).getByRole('button', { name: 'Add 1 competency' }))
    expect(onAddCompetencies).toHaveBeenCalledWith(null, ['c131'])
  })

  it('greys out competencies already in the list', async () => {
    const { user } = renderEditor()

    await user.click(await screen.findByRole('button', { name: 'Add existing competency' }))
    const add = screen.getByRole('dialog', { name: /Add existing competencies/ })
    expect(within(add).getByText('Already in this list')).toBeInTheDocument()
    expect(within(add).getByText('Not in any list')).toBeInTheDocument()
    expect(within(add).getByRole('button', { name: /4\.3\.1/ })).toHaveAttribute('aria-disabled', 'true')
  })
})
