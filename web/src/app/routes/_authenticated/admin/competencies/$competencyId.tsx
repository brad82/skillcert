import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../../../shell/ComingSoonPage'

/** `listId`: the list the editor was opened from, for the breadcrumb back. */
export type CompetencySearch = { listId?: string }

export const Route = createFileRoute('/_authenticated/admin/competencies/$competencyId')({
  validateSearch: (search: Record<string, unknown>): CompetencySearch => (typeof search.listId === 'string' ? { listId: search.listId } : {}),
  component: () => <ComingSoonPage title={<Trans>Competency</Trans>} />,
})
