import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import ButtonBase from '@mui/material/ButtonBase'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { CircleCheck, CircleX, Clock, FilePen, Hourglass, type LucideIcon } from 'lucide-react'
import type { MyHistoryEntryDto, MyReviewDto } from '@shared/api/model'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import type { CompetencyStatusKey } from '@shared/lib/theme'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useHistory } from '../../CompetencyProvider'
import { confirmationLabels, outcomeLabels } from './reviewText'

type Props = {
  entry: MyHistoryEntryDto
}

const historyEntryStyles = (key: CompetencyStatusKey) => ({
  row: { display: 'flex', alignItems: 'flex-start', gap: 3, py: 3, width: '100%', textAlign: 'left', justifyContent: 'flex-start' },
  dot: (theme: Theme) => ({
    width: 32,
    height: 32,
    flexShrink: 0,
    display: 'grid',
    placeItems: 'center',
    borderRadius: theme.radius.pill,
    bgcolor: theme.palette.status[key].bg,
    color: theme.palette.status[key].fg,
  }),
  body: { flexGrow: 1, minWidth: 0 },
  title: { fontWeight: 600 },
  date: (theme: Theme) => ({ ...theme.typography.date, color: 'text.secondary', flexShrink: 0 }),
})

/** Colour and icon for a review entry: the outcome, or pending/rejected when that's what matters. */
function reviewAppearance(review: MyReviewDto): { key: CompetencyStatusKey; icon: LucideIcon } {
  if (review.confirmationStatus === 'Pending') return { key: 'pending', icon: Hourglass }
  if (review.confirmationStatus === 'Rejected') return { key: 'notCertified', icon: CircleX }
  return review.outcome === 'Competent' ? { key: 'current', icon: CircleCheck } : { key: 'notCompetent', icon: CircleX }
}

/** One timeline row: a review (tap for detail) or a lapse (expiry, breaking revision). */
export function HistoryEntry({ entry }: Props) {
  const { openReview } = useHistory()
  const { i18n } = useLingui()
  const { locale } = useLocale()
  const review = entry.review
  const appearance = review
    ? reviewAppearance(review)
    : { key: (entry.kind === 'Expired' ? 'expired' : 'notCertified') as CompetencyStatusKey, icon: entry.kind === 'Expired' ? Clock : FilePen }
  const styles = historyEntryStyles(appearance.key)
  const Icon = appearance.icon
  const revisionNumber = entry.revisionNumber

  const content = (
    <>
      <Box sx={styles.dot}>
        <Icon size={18} aria-hidden />
      </Box>
      <Box sx={styles.body}>
        {review ? (
          <>
            <Typography sx={styles.title}>{i18n._(outcomeLabels[review.outcome])}</Typography>
            <Typography variant="body2" color="text.secondary">
              {review.reviewerName} · {i18n._(reviewerLabel(review.method, review.classificationCode))} · <Trans>Rev {review.revisionNumber}</Trans>
            </Typography>
            {confirmationLabels[review.confirmationStatus] && (
              <Typography variant="body2" color="text.secondary">
                {i18n._(confirmationLabels[review.confirmationStatus]!)}
              </Typography>
            )}
          </>
        ) : entry.kind === 'Expired' ? (
          <Typography sx={styles.title}>
            <Trans>Expired</Trans>
          </Typography>
        ) : (
          <>
            <Typography sx={styles.title}>
              <Trans>Revision {revisionNumber} published</Trans>
            </Typography>
            <Typography variant="body2" color="text.secondary">
              <Trans>Earlier sign-offs no longer count.</Trans>
            </Typography>
          </>
        )}
      </Box>
      <Typography component="span" variant="body2" sx={styles.date}>
        {formatDate(entry.at, locale)}
      </Typography>
    </>
  )

  if (!review) return <Box sx={styles.row}>{content}</Box>
  return (
    <ButtonBase sx={styles.row} onClick={() => openReview(review.id)}>
      {content}
    </ButtonBase>
  )
}
