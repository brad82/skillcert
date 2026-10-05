import { Plural, Trans } from '@lingui/react/macro'

/** "Every year", "Every 2 years", "Every 90 days" or "Never expires". */
export function Recertification({ days }: { days: number | null }) {
  if (days === null) return <Trans>Never expires</Trans>
  if (days % 365 === 0) {
    const years = days / 365
    return <Plural value={years} one="Every year" other="Every # years" />
  }
  return <Plural value={days} one="Every day" other="Every # days" />
}
