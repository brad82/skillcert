import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../../shell/ComingSoonPage'

export type AuditSearch = { entityId?: string }

export const Route = createFileRoute('/_authenticated/admin/audit')({
  validateSearch: (search: Record<string, unknown>): AuditSearch => (typeof search.entityId === 'string' ? { entityId: search.entityId } : {}),
  component: () => <ComingSoonPage title={<Trans>Audit log</Trans>} />,
})
