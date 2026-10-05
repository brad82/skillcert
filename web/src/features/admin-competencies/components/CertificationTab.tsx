import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import FormControl from '@mui/material/FormControl'
import FormControlLabel from '@mui/material/FormControlLabel'
import FormLabel from '@mui/material/FormLabel'
import Radio from '@mui/material/Radio'
import RadioGroup from '@mui/material/RadioGroup'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { type SigningAuthority, signingAuthorities } from '@shared/lib/reviewers'
import { useDraft } from '../CompetencyEditorProvider'

/** Recertification period and signing authority. Changing either always publishes a new revision. */
export function CertificationTab() {
  const draft = useDraft()
  const { t, i18n } = useLingui()
  return (
    <Stack spacing={6} role="tabpanel" id="panel-certification" aria-labelledby="tab-certification">
      <Alert severity="info"><Trans>Changing anything here always publishes a new revision, because it changes who is current.</Trans></Alert>
      <TextField
        label={t`Recertification period (days)`}
        inputMode="numeric"
        value={draft.recertificationDays}
        onChange={(e) => draft.setRecertificationDays(e.target.value)}
        error={draft.problems.recertificationDays !== null}
        helperText={draft.problems.recertificationDays ? t`Use a whole number from 1 to 36500.` : t`1 to 36500. Leave empty if the skill never expires.`}
        sx={{ maxWidth: 360 }}
      />
      <FormControl>
        <FormLabel id="authority-label"><Trans>Signing authority: lowest level that can sign</Trans></FormLabel>
        <RadioGroup aria-labelledby="authority-label" value={draft.authority} onChange={(e) => draft.setAuthority(e.target.value as SigningAuthority)}>
          {signingAuthorities.map((a) => (
            <FormControlLabel key={a.key} value={a.key} control={<Radio />} label={i18n._(a.label)} />
          ))}
        </RadioGroup>
      </FormControl>
    </Stack>
  )
}
