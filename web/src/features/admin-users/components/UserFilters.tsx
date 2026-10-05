import { useLingui } from '@lingui/react/macro'
import InputAdornment from '@mui/material/InputAdornment'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { Search } from 'lucide-react'
import type { UserShow } from '../model/useUserFilters'
import { useUserFilterFields } from '../UsersProvider'

const userFiltersStyles = () => ({
  search: { flex: '1 1 320px' },
  show: { flex: '0 1 240px', minWidth: 200 },
})

export function UserFilters() {
  const { search, setSearch, show, setShow } = useUserFilterFields()
  const { t } = useLingui()
  const styles = userFiltersStyles()

  return (
    <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap' }}>
      <TextField
        label={t`Search by name or email`}
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        sx={styles.search}
        slotProps={{ input: { startAdornment: <InputAdornment position="start"><Search size={18} aria-hidden /></InputAdornment> } }}
      />
      <TextField select label={t`Show`} value={show} onChange={(event) => setShow(event.target.value as UserShow)} sx={styles.show}>
        <MenuItem value="all">{t`Active and deactivated`}</MenuItem>
        <MenuItem value="active">{t`Active only`}</MenuItem>
        <MenuItem value="deactivated">{t`Deactivated only`}</MenuItem>
      </TextField>
    </Stack>
  )
}
