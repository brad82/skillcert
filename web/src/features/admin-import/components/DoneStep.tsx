import { Plural, Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { ButtonLink } from '@shared/components/RouterLinks'
import { useImportResult } from '../ImportProvider'

export function DoneStep() {
  const { result, target, another } = useImportResult()
  if (!result) return null
  return (
    <Stack spacing={4}>
      <Alert severity="success">
        <Plural value={result.created} one="Created # competency at revision 1:" other="Created # competencies at revision 1:" /> {result.codes.join(', ')}
        {target && <> <Trans>They were added to {target.title}.</Trans></>}
      </Alert>
      <Stack direction="row" spacing={2}>
        {target ? (
          <ButtonLink variant="contained" to="/admin/lists/$listId" params={{ listId: target.id }}><Trans>Open {target.title}</Trans></ButtonLink>
        ) : (
          <ButtonLink variant="contained" to="/admin/competencies"><Trans>See all competencies</Trans></ButtonLink>
        )}
        <Button variant="outlined" onClick={another}><Trans>Import another file</Trans></Button>
      </Stack>
    </Stack>
  )
}
