import { msg } from '@lingui/core/macro'
import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import type { ApprovalGroupDto } from '@shared/api/model'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import { approvalSignatureUrl } from '../model/approvalsQuery'
import { useApprovalActions } from '../ApprovalsProvider'

type Props = {
  group: ApprovalGroupDto
}

const outcomeLabels = { Competent: msg`Competent`, NotCompetent: msg`Not competent` }

const approvalCardStyles = () => ({
  card: (theme: Theme) => ({ border: 1, borderColor: 'divider', borderRadius: theme.radius.md, p: 4 }),
  item: { display: 'flex', alignItems: 'center', gap: 2, py: 2, borderTop: 1, borderColor: 'divider' },
  code: (theme: Theme) => ({ ...theme.typography.code, color: 'text.secondary', minWidth: 44 }),
  title: { flexGrow: 1, minWidth: 0 },
  chip: (competent: boolean) => (theme: Theme) => {
    const key = competent ? 'current' : 'notCompetent'
    return { bgcolor: theme.palette.status[key].bg, color: theme.palette.status[key].fg, fontWeight: 600 }
  },
  label: (theme: Theme) => ({ ...theme.typography.overline, color: 'text.secondary', display: 'block' }),
  signature: (theme: Theme) => ({
    width: '100%',
    maxHeight: 120,
    objectFit: 'contain',
    bgcolor: '#fff', // dark ink on paper-white in both modes
    border: 1,
    borderColor: 'divider',
    borderRadius: theme.radius.sm,
  }),
})

/** One sitting: candidate, date, the skills and verdicts, comment, signature; confirm or reject all of it. */
export function ApprovalCard({ group }: Props) {
  const { busy, disabled, confirm, reject } = useApprovalActions(group.signatureId)
  const { i18n, t } = useLingui()
  const { locale } = useLocale()
  const styles = approvalCardStyles()
  const candidate = group.candidateName
  const signedOn = formatDate(group.reviewedAt, locale)

  return (
    <Paper component="section" elevation={0} sx={styles.card} aria-label={t`${candidate}, ${signedOn}`}>
      <Stack spacing={3}>
        <div>
          <Typography variant="h2">{candidate}</Typography>
          <Typography variant="body2" color="text.secondary">
            <Trans>Signed {signedOn}</Trans>
          </Typography>
        </div>
        <Box>
          {group.items.map((item) => (
            <Box key={item.reviewId} sx={styles.item}>
              <Typography component="span" sx={styles.code}>
                {item.code}
              </Typography>
              <Typography component="span" variant="body2" sx={styles.title}>
                {item.shortTitle}
              </Typography>
              <Chip size="small" label={i18n._(outcomeLabels[item.outcome])} sx={styles.chip(item.outcome === 'Competent')} />
            </Box>
          ))}
        </Box>
        {group.comment && (
          <div>
            <Typography component="span" sx={styles.label}>
              <Trans>Comment</Trans>
            </Typography>
            <Typography>{group.comment}</Typography>
          </div>
        )}
        <div>
          <Typography component="span" sx={styles.label}>
            <Trans>Your signature</Trans>
          </Typography>
          <Box component="img" src={approvalSignatureUrl(group.signatureId)} alt={t`Signature on ${candidate}'s sign-off`} sx={styles.signature} />
        </div>
        <Stack direction="row" spacing={2}>
          <Button variant="outlined" color="error" onClick={reject} disabled={disabled}>
            <Trans>Reject</Trans>
          </Button>
          <Button variant="contained" onClick={confirm} disabled={disabled} sx={{ flexGrow: 1 }}>
            {busy ? <Trans>Saving…</Trans> : <Trans>Confirm all</Trans>}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  )
}
