import { Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Breadcrumbs from '@mui/material/Breadcrumbs'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Link2 } from 'lucide-react'
import { TextLink } from '@shared/components/RouterLinks'
import { formatDate } from '@shared/lib/dates'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { useCompetencyHeader } from '../CompetencyEditorProvider'

/** Breadcrumb back to the list it was opened from, code and title, the lists sharing it, and (de)activation. */
export function CompetencyHeader() {
  const { competency, fromList, otherLists, busy, error, notice, deactivate, reactivate } = useCompetencyHeader()
  const { locale } = useLocale()
  const { current } = competency
  const listCount = otherLists.length
  return (
    <Stack spacing={4}>
      <Breadcrumbs>
        {fromList ? <TextLink to="/admin/lists"><Trans>Competency lists</Trans></TextLink> : <TextLink to="/admin/competencies"><Trans>All competencies</Trans></TextLink>}
        {fromList && <TextLink to="/admin/lists/$listId" params={{ listId: fromList.id }}>{fromList.title}</TextLink>}
        <Typography variant="code" color="text.secondary">{competency.code}</Typography>
      </Breadcrumbs>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={3} sx={{ alignItems: 'baseline', flexWrap: 'wrap' }}>
            <Typography variant="code" color="text.secondary" sx={{ fontSize: 20 }}>{competency.code}</Typography>
            <Typography variant="h1">{current.title}</Typography>
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {competency.isActive ? <Trans>Active</Trans> : <Trans>Inactive</Trans>} ·{' '}
            <Trans>Revision {current.number}, published {formatDate(current.publishedAt, locale)}</Trans>
          </Typography>
          {listCount > 1 && (
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Link2 size={16} aria-hidden />
              <Typography variant="body2">
                <Trans>
                  <strong>Shared by {listCount} lists:</strong> {otherLists.map((l) => l.title).join(', ')}. Changes apply to all of them.
                </Trans>
              </Typography>
            </Stack>
          )}
        </Stack>
        {competency.isActive ? (
          <Button variant="outlined" disabled={busy} onClick={deactivate}><Trans>Deactivate</Trans></Button>
        ) : (
          <Button variant="outlined" disabled={busy} onClick={reactivate}><Trans>Reactivate</Trans></Button>
        )}
      </Stack>
      {notice && <Alert severity="success">{notice}</Alert>}
      {error && <Alert severity="error">{error}</Alert>}
    </Stack>
  )
}
