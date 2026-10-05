import { Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { Plus } from 'lucide-react'
import { TextLink } from '@shared/components/RouterLinks'
import { ListsTable } from '../components/ListsTable'
import { NewListDialog } from '../components/NewListDialog'
import { useListsTable, useNewList } from '../ListsProvider'

/** Competency lists: the way into editing. One list per skill set. Layout only. */
export function ListsPage() {
  const { search, setSearch } = useListsTable()
  const { start } = useNewList()
  const { t } = useLingui()
  return (
    <Stack spacing={6}>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <div>
          <Typography variant="h1"><Trans>Competency lists</Trans></Typography>
          <Typography variant="body2" color="text.secondary"><Trans>One list per skill set. Open a list to edit its competencies.</Trans></Typography>
        </div>
        <Button variant="contained" startIcon={<Plus size={20} aria-hidden />} onClick={start}><Trans>New list</Trans></Button>
      </Stack>
      <TextField label={t`Search lists`} type="search" value={search} onChange={(e) => setSearch(e.target.value)} sx={{ maxWidth: 420 }} />
      <ListsTable />
      <Typography variant="body2" color="text.secondary">
        <Trans>Looking for a competency that isn't in any list? Use <TextLink to="/admin/competencies">All competencies</TextLink>.</Trans>
      </Typography>
      <NewListDialog />
    </Stack>
  )
}
