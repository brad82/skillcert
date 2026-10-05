import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { MyArchivedRecordDto } from '@shared/api/model'
import { afaFragment } from '../../../test/fixtures'
import { TestProviders } from '../../../test/TestProviders'
import { RecordsProvider } from '../RecordsProvider'
import { RecordsPage } from './RecordsPage'

const archive: MyArchivedRecordDto = {
  id: 'rec-1',
  listId: 'list-afa',
  listTitle: 'AFA Skills Record',
  completionDate: '2026-09-01T12:00:00Z',
  generatedAt: '2026-09-02T08:00:00Z',
}

function renderRecords(archives: MyArchivedRecordDto[] = [archive]) {
  render(
    <TestProviders>
      <RecordsProvider
        lists={[afaFragment()]}
        archives={archives}
        currentRecordUrl={(id) => `/current/${id}`}
        archivedRecordUrl={(id) => `/archived/${id}`}
      >
        <RecordsPage />
      </RecordsProvider>
    </TestProviders>,
  )
}

describe('RecordsPage', () => {
  it('offers the current record of each list and every archived record as downloads', async () => {
    renderRecords()

    const current = await screen.findByRole('link', { name: 'Download current record (PDF)' })
    expect(current).toHaveAttribute('href', '/current/list-afa')
    expect(current).toHaveAttribute('download')
    expect(screen.getByText(/Not compliant/)).toBeInTheDocument()

    const archived = screen.getByRole('region', { name: 'Archived records' })
    expect(within(archived).getByText('Completed Sep 1, 2026 · saved Sep 2, 2026')).toBeInTheDocument()
    expect(within(archived).getByRole('link', { name: 'Download AFA Skills Record, completed Sep 1, 2026' })).toHaveAttribute('href', '/archived/rec-1')
  })

  it('explains archives before there are any', async () => {
    renderRecords([])

    expect(await screen.findByText(/saved here automatically each time you become compliant/)).toBeInTheDocument()
  })
})
