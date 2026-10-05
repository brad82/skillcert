import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import { ComingSoonPage } from '../../shell/ComingSoonPage'

export const Route = createFileRoute('/_authenticated/skills_/$competencyId')({
  component: () => <ComingSoonPage title={<Trans>Skill detail</Trans>} />,
})
