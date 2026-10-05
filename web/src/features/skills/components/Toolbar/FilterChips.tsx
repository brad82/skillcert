import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import type { PresentationState } from '@shared/lib/competencyStatus'
import type { QuickFilter } from '../../model/skillsTree'
import { useSkillsToolbar } from '../../SkillsProvider'

const quickFilters: { value: QuickFilter; label: MessageDescriptor }[] = [
  { value: 'all', label: msg`All` },
  { value: 'needsAction', label: msg`Needs action` },
  { value: 'current', label: msg`Current` },
]

const stateLabels: Record<PresentationState, MessageDescriptor> = {
  current: msg`Current`,
  expiringSoon: msg`Expiring soon`,
  expired: msg`Expired`,
  notCompetent: msg`Not competent`,
  pending: msg`Pending`,
  notCertified: msg`Not certified`,
}

/** All · Needs action · Current, plus the state picked on Home (removable). */
export function FilterChips() {
  const { filter, setQuick, clearStatus } = useSkillsToolbar()
  const { i18n, t } = useLingui()

  return (
    <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
      {quickFilters.map(({ value, label }) => (
        <Chip
          key={value}
          label={i18n._(label)}
          color={filter.quick === value ? 'primary' : 'default'}
          variant={filter.quick === value ? 'filled' : 'outlined'}
          onClick={() => setQuick(value)}
          aria-pressed={filter.quick === value}
        />
      ))}
      {filter.status ? (
        <Chip label={i18n._(stateLabels[filter.status])} onClick={clearStatus} onDelete={clearStatus} color="secondary" aria-label={t`Remove filter: ${i18n._(stateLabels[filter.status])}`} />
      ) : null}
    </Stack>
  )
}
