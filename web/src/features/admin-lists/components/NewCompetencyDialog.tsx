import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { type SigningAuthority, signingAuthorities } from '@shared/lib/reviewers'
import { useNewCompetencyDialog } from '../ListEditorProvider'
import { EditorDialog } from './EditorDialog'
import { ParentPicker } from './ParentPicker'

/**
 * Creates a competency straight into this list. A code already in use is refused, and the dialog offers to
 * add that existing competency instead, which shares it between lists.
 */
export function NewCompetencyDialog() {
  const { open, listTitle, draft, parents, busy, error, close, submit, addExistingInstead } = useNewCompetencyDialog()
  const { t, i18n } = useLingui()
  const show = draft.showErrors
  return (
    <EditorDialog
      open={open}
      title={<Trans>New competency in {listTitle}</Trans>}
      subtitle={<Trans>You can add the description and resources after it's created.</Trans>}
      error={draft.duplicateOf ? null : error}
      submitLabel={<Trans>Create and add to list</Trans>}
      canSubmit
      busy={busy}
      onClose={close}
      onSubmit={() => void submit()}
    >
      {draft.duplicateOf && (
        <Alert severity="error" action={<Button color="inherit" onClick={addExistingInstead}><Trans>Add existing</Trans></Button>}>
          <Trans>{draft.duplicateOf} already exists. To use it in this list, add it as an existing competency; it will then be shared by both lists.</Trans>
        </Alert>
      )}
      <ParentPicker label={t`Under heading`} value={draft.parentId} onChange={draft.setParentId} headings={parents} />
      <Stack direction="row" spacing={3}>
        <TextField
          label={t`Code`}
          required
          autoFocus
          value={draft.code}
          onChange={(e) => draft.setCode(e.target.value)}
          error={(show && draft.problems.code !== null) || draft.duplicateOf !== null}
          helperText={show && draft.problems.code ? t`Enter a code.` : ' '}
          sx={{ width: 140 }}
        />
        <TextField
          label={t`Title`}
          required
          value={draft.title}
          onChange={(e) => draft.setTitle(e.target.value)}
          error={show && draft.problems.title !== null}
          helperText={show && draft.problems.title ? t`Enter a title.` : ' '}
          slotProps={{ htmlInput: { maxLength: 300 } }}
          sx={{ flexGrow: 1 }}
        />
      </Stack>
      <TextField label={t`Short title`} value={draft.shortTitle} onChange={(e) => draft.setShortTitle(e.target.value)} helperText={t`Uses the title when empty.`} slotProps={{ htmlInput: { maxLength: 300 } }} />
      <Stack direction="row" spacing={3}>
        <TextField
          label={t`Recertification period (days)`}
          inputMode="numeric"
          value={draft.recertificationDays}
          onChange={(e) => draft.setRecertificationDays(e.target.value)}
          error={show && draft.problems.recertificationDays !== null}
          helperText={show && draft.problems.recertificationDays ? t`Use a whole number from 1 to 36500.` : t`Empty: never expires.`}
          sx={{ flex: 1 }}
        />
        <TextField select label={t`Signing authority`} value={draft.authority} onChange={(e) => draft.setAuthority(e.target.value as SigningAuthority)} sx={{ flex: 1 }}>
          {signingAuthorities.map((a) => (
            <MenuItem key={a.key} value={a.key}>{i18n._(a.label)}</MenuItem>
          ))}
        </TextField>
      </Stack>
    </EditorDialog>
  )
}
