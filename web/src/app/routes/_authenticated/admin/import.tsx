import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../../shell/ComingSoonPage'

export const Route = createFileRoute('/_authenticated/admin/import')({
  component: () => <ComingSoonPage title={<Trans>CSV import</Trans>} />,
})
