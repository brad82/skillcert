import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useOverview } from '../../CompetencyProvider'

const pendingNoticeStyles = () => ({
  box: (theme: Theme) => ({
    border: '1px dashed',
    borderColor: theme.palette.status.pending.fg,
    bgcolor: theme.palette.status.pending.bg,
    color: theme.palette.status.pending.fg,
    borderRadius: theme.radius.md,
    p: 4,
  }),
})

/** "Signed <date> by <name> (Supervisor). Becomes Current once <name> confirms." (wireframe 3c). */
export function PendingNotice() {
  const { pendingReview } = useOverview()
  const { i18n } = useLingui()
  const { locale } = useLocale()
  const styles = pendingNoticeStyles()
  if (!pendingReview) return null

  const signedOn = formatDate(pendingReview.reviewedAt, locale)
  const name = pendingReview.reviewerName
  const method = i18n._(reviewerLabel(pendingReview.method, pendingReview.classificationCode))

  return (
    <Box role="note" sx={styles.box}>
      <Typography>
        <Trans>
          Signed {signedOn} by {name} ({method}). Becomes Current once {name} confirms.
        </Trans>
      </Typography>
    </Box>
  )
}
