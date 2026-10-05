import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Drawer from '@mui/material/Drawer'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { X } from 'lucide-react'
import { signatureUrl } from '@features/my-record'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useReviewSheetState } from '../../CompetencyProvider'
import { confirmationLabels, outcomeLabels } from './reviewText'

const reviewSheetStyles = () => ({
  paper: (theme: Theme) => ({ borderTopLeftRadius: theme.radius.md, borderTopRightRadius: theme.radius.md, maxWidth: 720, mx: 'auto' }),
  body: { p: 4, pb: 'calc(16px + env(safe-area-inset-bottom, 0px))', maxHeight: '85dvh', overflowY: 'auto' },
  header: { display: 'flex', alignItems: 'center', justifyContent: 'space-between' },
  label: (theme: Theme) => ({ ...theme.typography.overline, color: 'text.secondary', display: 'block' }),
  signature: (theme: Theme) => ({
    width: '100%',
    maxHeight: 160,
    objectFit: 'contain',
    bgcolor: '#fff', // signatures are dark strokes on white in both modes, like paper
    border: 1,
    borderColor: 'divider',
    borderRadius: theme.radius.sm,
  }),
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', mr: 2 }),
})

/** A review's detail: reviewer, outcome, confirmation, comment, signature and the skills signed with it. */
export function ReviewSheet() {
  const { review, close } = useReviewSheetState()
  const { i18n, t } = useLingui()
  const { locale } = useLocale()
  const styles = reviewSheetStyles()

  return (
    <Drawer anchor="bottom" open={review !== null} onClose={close} slotProps={{ paper: { sx: styles.paper } }}>
      {review && (
        <Stack spacing={4} sx={styles.body} role="dialog" aria-label={t`Review detail`}>
          <Box sx={styles.header}>
            <Typography variant="h2">{i18n._(outcomeLabels[review.outcome])}</Typography>
            <IconButton aria-label={t`Close`} onClick={close}>
              <X size={20} aria-hidden />
            </IconButton>
          </Box>
          <div>
            <Typography component="span" sx={styles.label}>
              <Trans>Reviewer</Trans>
            </Typography>
            <Typography>
              {review.reviewerName} · {i18n._(reviewerLabel(review.method, review.classificationCode))}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {formatDate(review.reviewedAt, locale)} · <Trans>Rev {review.revisionNumber}</Trans>
            </Typography>
          </div>
          {confirmationLabels[review.confirmationStatus] && (
            <div>
              <Typography component="span" sx={styles.label}>
                <Trans>Confirmation</Trans>
              </Typography>
              <Typography>
                {i18n._(confirmationLabels[review.confirmationStatus]!)}
                {review.decidedAt && ` · ${formatDate(review.decidedAt, locale)}`}
              </Typography>
              {review.rejectionReason && <Typography color="text.secondary">{review.rejectionReason}</Typography>}
            </div>
          )}
          {review.comment && (
            <div>
              <Typography component="span" sx={styles.label}>
                <Trans>Comment</Trans>
              </Typography>
              <Typography>{review.comment}</Typography>
            </div>
          )}
          {review.signatureId && (
            <div>
              <Typography component="span" sx={styles.label}>
                <Trans>Signature</Trans>
              </Typography>
              <Box component="img" src={signatureUrl(review.signatureId)} alt={t`Signature of ${review.reviewerName}`} sx={styles.signature} />
            </div>
          )}
          {review.signedWith.length > 0 && (
            <div>
              <Typography component="span" sx={styles.label}>
                <Trans>Signed in the same sitting</Trans>
              </Typography>
              {review.signedWith.map((other) => (
                <Typography key={other.competencyId} variant="body2">
                  <Box component="span" sx={styles.code}>
                    {other.code}
                  </Box>
                  {other.shortTitle}
                </Typography>
              ))}
            </div>
          )}
        </Stack>
      )}
    </Drawer>
  )
}
