import { Trans } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import { useHistory } from '../../CompetencyProvider'
import { HistoryEntry } from './HistoryEntry'

const historyTabStyles = () => ({
  list: { listStyle: 'none', m: 0, p: 0, '& > li + li': { borderTop: 1, borderColor: 'divider' } },
})

/** Wireframe 3b: reviews and lapses, newest first. */
export function HistoryTab() {
  const { entries } = useHistory()
  const styles = historyTabStyles()

  if (entries.length === 0) {
    return (
      <Typography color="text.secondary">
        <Trans>Not reviewed yet.</Trans>
      </Typography>
    )
  }

  return (
    <Box component="ul" sx={styles.list}>
      {entries.map((entry) => (
        <li key={entry.review?.id ?? `${entry.kind}-${entry.at}`}>
          <HistoryEntry entry={entry} />
        </li>
      ))}
    </Box>
  )
}
