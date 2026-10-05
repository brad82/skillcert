import { Trans } from '@lingui/react/macro'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { TextLink } from '@shared/components/RouterLinks'
import { formatDate } from '@shared/lib/dates'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { useRevisionHistory } from '../CompetencyEditorProvider'

const cardStyles = () => ({ card: { p: 4 } })

/** Revisions (newest first), the lists that use the competency, and its audit history. */
export function RevisionHistory() {
  const { revisions, lists, competencyId, current } = useRevisionHistory()
  const { locale } = useLocale()
  const styles = cardStyles()
  return (
    <Stack spacing={6} sx={{ flex: '1 1 300px', minWidth: 0 }}>
      <Paper variant="outlined" sx={styles.card} component="section" aria-labelledby="revisions-heading">
        <Typography id="revisions-heading" variant="h3" sx={{ mb: 2 }}><Trans>Revisions</Trans></Typography>
        <Stack component="ol" divider={<Paper variant="outlined" sx={{ borderWidth: 0, borderTopWidth: 1 }} />} sx={{ listStyle: 'none', m: 0, p: 0 }}>
          {revisions.map((r) => (
            <Stack component="li" key={r.number} spacing={0.5} sx={{ py: 3 }}>
              <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {r.number === current ? <Trans>Revision {r.number} · current</Trans> : <Trans>Revision {r.number}</Trans>}
                </Typography>
                <Typography variant="date" color="text.secondary">{formatDate(r.publishedAt, locale)}</Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {r.invalidatesPreviousReviews ? <Trans>Required reassessment</Trans> : <Trans>Kept existing sign-offs</Trans>}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Paper>
      <Paper variant="outlined" sx={styles.card} component="section" aria-labelledby="lists-heading">
        <Typography id="lists-heading" variant="h3" sx={{ mb: 2 }}><Trans>Used in lists</Trans></Typography>
        {lists.length === 0 ? (
          <Typography variant="body2" color="text.secondary"><Trans>Not in any list.</Trans></Typography>
        ) : (
          <Stack component="ul" spacing={1} sx={{ m: 0, pl: 5 }}>
            {lists.map((l) => (
              <li key={l.id}><TextLink to="/admin/lists/$listId" params={{ listId: l.id }} variant="body2">{l.title}</TextLink></li>
            ))}
          </Stack>
        )}
      </Paper>
      <Paper variant="outlined" sx={styles.card}>
        <Typography variant="h3" sx={{ mb: 2 }}><Trans>History</Trans></Typography>
        <TextLink to="/admin/audit" search={{ entityId: competencyId }} variant="body2"><Trans>Open in the audit log</Trans></TextLink>
      </Paper>
    </Stack>
  )
}
