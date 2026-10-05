import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../shell/ComingSoonPage'

export const Route = createFileRoute('/_authenticated/events')({
  component: () => <ComingSoonPage title={<Trans>Events</Trans>} />,
})
