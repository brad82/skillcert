import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import InputAdornment from '@mui/material/InputAdornment'
import List from '@mui/material/List'
import ListItemButton from '@mui/material/ListItemButton'
import ListItemText from '@mui/material/ListItemText'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { Search } from 'lucide-react'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useReviewerSearch } from '../SignOffProvider'

/** Reviewer step 1: "Find your name" among registered users who can sign the whole group (spec §10). */
export function FindReviewer() {
  const { query, setQuery, reviewers, choose } = useReviewerSearch()
  const { i18n, t } = useLingui()

  return (
    <Stack spacing={4}>
      <Typography variant="h1">
        <Trans>Find your name</Trans>
      </Typography>
      <TextField
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        label={t`Search reviewers`}
        autoFocus
        slotProps={{ input: { startAdornment: <InputAdornment position="start"><Search size={18} aria-hidden /></InputAdornment> } }}
      />
      {reviewers.length === 0 ? (
        <Typography color="text.secondary">
          <Trans>No registered reviewer matches. Only people who can sign every skill here are listed.</Trans>
        </Typography>
      ) : (
        <List disablePadding>
          {reviewers.map((reviewer) => (
            <ListItemButton key={reviewer.userId} onClick={() => choose(reviewer)} divider>
              <ListItemText primary={reviewer.displayName} />
              <Chip size="small" label={i18n._(reviewerLabel(reviewer.method, reviewer.classificationCode))} />
            </ListItemButton>
          ))}
        </List>
      )}
    </Stack>
  )
}
