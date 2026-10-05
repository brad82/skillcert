import { useLingui } from '@lingui/react/macro'
import InputAdornment from '@mui/material/InputAdornment'
import TextField from '@mui/material/TextField'
import { Search } from 'lucide-react'
import { useSkillsToolbar } from '../../SkillsProvider'

export function SearchField() {
  const { filter, setText } = useSkillsToolbar()
  const { t } = useLingui()

  return (
    <TextField
      type="search"
      size="small"
      fullWidth
      placeholder={t`Search skills`}
      value={filter.text}
      onChange={(event) => setText(event.target.value)}
      slotProps={{
        htmlInput: { 'aria-label': t`Search skills` },
        input: { startAdornment: <InputAdornment position="start"><Search size={18} aria-hidden /></InputAdornment> },
      }}
    />
  )
}
