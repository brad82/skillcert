import { useLingui } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import type { ListDetailsDraft } from '../model/useListDetailsDraft'

/** Title and description inputs, shared by the new-list and rename-list dialogs. */
export function ListDetailsFields({ draft }: { draft: ListDetailsDraft }) {
  const { t } = useLingui()
  return (
    <Stack spacing={4} sx={{ pt: 2 }}>
      <TextField label={t`Title`} required autoFocus value={draft.title} onChange={(e) => draft.setTitle(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
      <TextField label={t`Description`} multiline minRows={2} value={draft.description} onChange={(e) => draft.setDescription(e.target.value)} />
    </Stack>
  )
}
