import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import type { Theme } from '@mui/material/styles'
import { CircleCheck, CircleDashed, CircleX, Clock, Hourglass, TriangleAlert, type LucideIcon } from 'lucide-react'
import type { CurrencyDto } from '@shared/api/model'
import { type PresentationState, presentationState } from '@shared/lib/competencyStatus'
import { formatDate } from '@shared/lib/dates'
import type { CompetencyStatusKey } from '@shared/lib/theme'
import { useLocale } from '@shared/i18n/AppI18nProvider'

type Props = {
  currency: CurrencyDto
}

const appearance: Record<PresentationState, { colours: CompetencyStatusKey; icon: LucideIcon; label: MessageDescriptor }> = {
  current: { colours: 'current', icon: CircleCheck, label: msg`Current` },
  expiringSoon: { colours: 'current', icon: Clock, label: msg`Expiring soon` },
  expired: { colours: 'expired', icon: TriangleAlert, label: msg`Expired` },
  notCompetent: { colours: 'notCompetent', icon: CircleX, label: msg`Not competent` },
  pending: { colours: 'pending', icon: Hourglass, label: msg`Pending` },
  notCertified: { colours: 'notCertified', icon: CircleDashed, label: msg`Not certified` },
}

const statusChipStyles = (key: CompetencyStatusKey) => ({
  chip: (theme: Theme) => ({
    bgcolor: theme.palette.status[key].bg,
    color: theme.palette.status[key].fg,
    fontWeight: 600,
    flexShrink: 0,
    '& .MuiChip-icon': { color: 'inherit', ml: 2 },
  }),
})

/**
 * A competency's status as the design system draws it: status colours, an icon and a label, never colour
 * alone. Expiring soon shows its date ("Expires 9 Nov 2026") in Current colours.
 */
export function StatusChip({ currency }: Props) {
  const { i18n, t } = useLingui()
  const { locale } = useLocale()
  const state = presentationState(currency)
  const { colours, icon: Icon, label } = appearance[state]
  const styles = statusChipStyles(colours)
  const text =
    state === 'expiringSoon' && currency.expiresAt
      ? t`Expires ${formatDate(currency.expiresAt, locale)}`
      : i18n._(label)

  return <Chip size="small" icon={<Icon size={14} aria-hidden />} label={text} sx={styles.chip} />
}
