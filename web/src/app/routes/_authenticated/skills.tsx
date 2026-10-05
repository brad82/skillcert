import { Trans } from '@lingui/react/macro'
import { createFileRoute } from '@tanstack/react-router'
import type { PresentationState } from '@shared/lib/competencyStatus'
import { ComingSoonPage } from '../../shell/ComingSoonPage'

export type SkillsSearch = { status?: PresentationState }

const states: PresentationState[] = ['current', 'expiringSoon', 'expired', 'notCompetent', 'pending', 'notCertified']

export const Route = createFileRoute('/_authenticated/skills')({
  validateSearch: (search: Record<string, unknown>): SkillsSearch => ({
    status: states.includes(search.status as PresentationState) ? (search.status as PresentationState) : undefined,
  }),
  component: () => <ComingSoonPage title={<Trans>Skills</Trans>} />,
})
