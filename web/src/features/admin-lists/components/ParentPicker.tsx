import { useLingui } from '@lingui/react/macro'
import Autocomplete from '@mui/material/Autocomplete'
import TextField from '@mui/material/TextField'
import type { AdminListNodeDto } from '@shared/api/model'
import { nodeLabel } from '../model/tree'

type Props = {
  label: string
  value: string | null
  onChange: (parentId: string | null) => void
  headings: AdminListNodeDto[]
}

type Option = { id: string | null; label: string }

/** Where in the list something goes: the top level or a heading. Typing filters the headings. */
export function ParentPicker({ label, value, onChange, headings }: Props) {
  const { t } = useLingui()
  const options: Option[] = [{ id: null, label: t`Top level of the list` }, ...headings.map((h) => ({ id: h.id, label: nodeLabel(h) }))]
  return (
    <Autocomplete
      options={options}
      value={options.find((o) => o.id === value) ?? options[0]}
      onChange={(_, option) => onChange(option?.id ?? null)}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      disableClearable
      renderInput={(params) => <TextField {...params} label={label} />}
    />
  )
}
