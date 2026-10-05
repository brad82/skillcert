import { useLingui } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useDraft } from '../CompetencyEditorProvider'

export function DetailsTab() {
  const draft = useDraft()
  const { t } = useLingui()
  const { problems } = draft
  return (
    <Stack spacing={6} role="tabpanel" id="panel-details" aria-labelledby="tab-details">
      <TextField
        label={t`Title`}
        required
        value={draft.title}
        onChange={(e) => draft.setTitle(e.target.value)}
        error={problems.title !== null}
        helperText={problems.title === 'required' ? t`Enter a title.` : `${draft.title.length} / 300`}
      />
      <TextField
        label={t`Short title`}
        value={draft.shortTitle}
        onChange={(e) => draft.setShortTitle(e.target.value)}
        error={problems.shortTitle !== null}
        helperText={t`The paper record's wording, shown under its heading. Uses the title when empty.`}
      />
      <TextField
        label={t`Description`}
        multiline
        minRows={5}
        value={draft.description}
        onChange={(e) => draft.setDescription(e.target.value)}
        error={problems.description !== null}
        helperText={`${draft.description.length} / 4000`}
      />
    </Stack>
  )
}
