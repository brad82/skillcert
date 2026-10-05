import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../../shell/ComingSoonPage'

export const Route = createFileRoute('/_authenticated/admin/competencies')({
  component: () => <ComingSoonPage title={<Trans>All competencies</Trans>} />,
})
