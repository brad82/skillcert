import { Trans } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { BasketBar } from '../components/BasketBar'
import { ListSection } from '../components/ListSection'
import { FilterChips } from '../components/Toolbar/FilterChips'
import { SearchField } from '../components/Toolbar/SearchField'
import { useSkillsLists } from '../SkillsProvider'

/** Wireframe 2a: search, filter chips, then each required list as an accordion tree. Layout only. */
export function SkillsPage() {
  const views = useSkillsLists()

  return (
    <Stack spacing={3} sx={{ pb: 16 }}>
      <Typography variant="h1">{views.length === 1 ? views[0]!.list.title : <Trans>Skills</Trans>}</Typography>
      <SearchField />
      <FilterChips />
      {views.length === 0 ? (
        <Typography color="text.secondary">
          <Trans>You have no required skills records yet.</Trans>
        </Typography>
      ) : (
        views.map((view) => <ListSection key={view.list.id} view={view} showTitle={views.length > 1} />)
      )}
      <BasketBar />
    </Stack>
  )
}
