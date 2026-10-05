import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { CompetencyImportPreviewResponse } from '@shared/api/model'
import { TestProviders } from '../../../test/TestProviders'
import { TestRouter } from '../../../test/TestRouter'
import { ImportProvider } from '../ImportProvider'
import { ImportPage } from './ImportPage'

const preview: CompetencyImportPreviewResponse = {
  canImport: false,
  fileErrors: [],
  rows: [
    { line: 2, code: '13.1', title: 'Avalanche transceiver search', lowestReviewer: { method: 'Classified', classificationCode: 'Instructor', order: 2 }, recertificationDays: 365, resourceCount: 1, errors: [], warnings: [] },
    { line: 3, code: '13.1', title: 'Multiple burial search', lowestReviewer: null, recertificationDays: null, resourceCount: 0, errors: ['Code 13.1 is repeated on line 2.'], warnings: [] },
  ],
}

function renderImport(shown: CompetencyImportPreviewResponse | null) {
  const callbacks = { onPreview: vi.fn(), onImport: vi.fn(), onReset: vi.fn(), onTargetListChange: vi.fn() }
  render(
    <TestProviders>
      <TestRouter>
        <ImportProvider lists={[]} targetListId={null} headings={[]} busy={false} error={null} preview={shown} result={null} {...callbacks}>
          <ImportPage />
        </ImportProvider>
      </TestRouter>
    </TestProviders>,
  )
  return { ...callbacks, user: userEvent.setup() }
}

describe('ImportPage', () => {
  it('reads the chosen file in the browser and sends its text for a preview', async () => {
    const { user, onPreview } = renderImport(null)

    const file = new File(['Code,Title\n13.1,Probe line'], 'avalanche.csv', { type: 'text/csv' })
    await user.upload(await screen.findByLabelText('Choose file'), file)
    expect(onPreview).toHaveBeenCalledWith('Code,Title\n13.1,Probe line')
  })

  it('keeps Import off while any row has an error, and filters to the errors', async () => {
    const { user } = renderImport(preview)

    expect(await screen.findByRole('button', { name: 'Import 2 competencies' })).toBeDisabled()
    const rows = screen.getByRole('table', { name: 'Rows' })
    await user.click(screen.getByRole('button', { name: 'Errors 1' }))
    expect(within(rows).queryByText('Avalanche transceiver search')).not.toBeInTheDocument()
    expect(within(rows).getByText(/repeated on line 2/)).toBeInTheDocument()
  })
})
