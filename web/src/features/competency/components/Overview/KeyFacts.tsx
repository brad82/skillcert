import { Plural, Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { daysUntil, formatDate } from '@shared/lib/dates'
import { reviewerLabel } from '@shared/lib/reviewers'
import { recertificationPeriod } from '../../model/competencyFacts'
import { useOverview } from '../../CompetencyProvider'

const keyFactsStyles = () => ({
  list: { display: 'grid', gridTemplateColumns: 'auto 1fr', columnGap: 4, rowGap: 2, m: 0 },
  label: (theme: Theme) => ({ ...theme.typography.overline, color: 'text.secondary', alignSelf: 'center' }),
  value: { m: 0 },
  date: (theme: Theme) => ({ ...theme.typography.date }),
})

/** Label/value pairs: last signed, by, expires, recertify, revision (wireframe 3c). */
export function KeyFacts() {
  const { effectiveReview, currency, revision } = useOverview()
  const { i18n } = useLingui()
  const { locale } = useLocale()
  const styles = keyFactsStyles()
  const period = recertificationPeriod(revision.recertificationDays)
  const date = (iso: string) => (
    <Box component="span" sx={styles.date}>
      {formatDate(iso, locale)}
    </Box>
  )

  const revisionNumber = revision.number
  const publishedOn = date(revision.publishedAt)
  const expires: ReactNode = currency.expiresAt ? (
    <>
      {date(currency.expiresAt)} · <DaysLeft days={daysUntil(currency.expiresAt)} />
    </>
  ) : currency.status === 'Current' ? (
    <Trans>Never</Trans>
  ) : (
    '—'
  )

  return (
    <Box component="dl" sx={styles.list}>
      <Fact label={<Trans>Last signed</Trans>}>{effectiveReview ? date(effectiveReview.reviewedAt) : <Trans>Never</Trans>}</Fact>
      <Fact label={<Trans>By</Trans>}>
        {effectiveReview
          ? `${effectiveReview.reviewerName} · ${i18n._(reviewerLabel(effectiveReview.method, effectiveReview.classificationCode))}`
          : '—'}
      </Fact>
      <Fact label={<Trans>Expires</Trans>}>{expires}</Fact>
      <Fact label={<Trans>Recertify</Trans>}>
        {period === null ? (
          <Trans>No expiry</Trans>
        ) : period.unit === 'year' ? (
          <Plural value={period.count} one="Every # year" other="Every # years" />
        ) : (
          <Plural value={period.count} one="Every # day" other="Every # days" />
        )}
      </Fact>
      <Fact label={<Trans>Revision</Trans>}>
        <Trans>
          {revisionNumber} · published {publishedOn}
        </Trans>
      </Fact>
    </Box>
  )
}

function Fact({ label, children }: { label: ReactNode; children: ReactNode }) {
  const styles = keyFactsStyles()
  return (
    <>
      <Typography component="dt" sx={styles.label}>
        {label}
      </Typography>
      <Typography component="dd" sx={styles.value}>
        {children}
      </Typography>
    </>
  )
}

function DaysLeft({ days }: { days: number }) {
  if (days >= 0) return <Plural value={days} one="# day left" other="# days left" />
  return <Plural value={-days} one="# day ago" other="# days ago" />
}
