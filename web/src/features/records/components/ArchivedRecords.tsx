import { Trans, useLingui } from '@lingui/react/macro'
import IconButton from '@mui/material/IconButton'
import List from '@mui/material/List'
import ListItem from '@mui/material/ListItem'
import ListItemText from '@mui/material/ListItemText'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Download } from 'lucide-react'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { formatDate } from '@shared/lib/dates'
import { useArchivedRecords } from '../RecordsProvider'

/** Records saved automatically when the candidate became compliant (spec §22), newest first. */
export function ArchivedRecords() {
  const archives = useArchivedRecords()
  const { t } = useLingui()
  const { locale } = useLocale()

  return (
    <Stack spacing={2} component="section" aria-labelledby="archived-records">
      <Typography variant="h2" id="archived-records">
        <Trans>Archived records</Trans>
      </Typography>
      {archives.length === 0 ? (
        <Typography color="text.secondary">
          <Trans>A copy is saved here automatically each time you become compliant with a record.</Trans>
        </Typography>
      ) : (
        <List disablePadding>
          {archives.map(({ archive, downloadUrl }) => {
            const completed = formatDate(archive.completionDate, locale)
            const saved = formatDate(archive.generatedAt, locale)
            return (
              <ListItem
                key={archive.id}
                divider
                disableGutters
                secondaryAction={
                  <IconButton edge="end" href={downloadUrl} download aria-label={t`Download ${archive.listTitle}, completed ${completed}`}>
                    <Download size={20} aria-hidden />
                  </IconButton>
                }
              >
                <ListItemText primary={archive.listTitle} secondary={t`Completed ${completed} · saved ${saved}`} />
              </ListItem>
            )
          })}
        </List>
      )}
    </Stack>
  )
}
