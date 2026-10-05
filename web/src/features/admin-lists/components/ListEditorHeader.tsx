import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Breadcrumbs from '@mui/material/Breadcrumbs'
import Button from '@mui/material/Button'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Pencil, Plus } from 'lucide-react'
import { ButtonLink, TextLink } from '@shared/components/RouterLinks'
import { useListHeader } from '../ListEditorProvider'

/** Breadcrumb, title (renamable), groups, and the toolbar that adds headings and competencies. */
export function ListEditorHeader() {
  const { list, error, rename, addHeading, addExisting, newCompetency } = useListHeader()
  const { t } = useLingui()
  return (
    <Stack spacing={4}>
      <Breadcrumbs>
        <TextLink to="/admin/lists"><Trans>Competency lists</Trans></TextLink>
        <Typography color="text.secondary">{list.title}</Typography>
      </Breadcrumbs>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Typography variant="h1">{list.title}</Typography>
            <IconButton aria-label={t`Rename list`} onClick={rename}><Pencil size={20} aria-hidden /></IconButton>
          </Stack>
          {list.description && <Typography color="text.secondary">{list.description}</Typography>}
          <Typography variant="body2" color="text.secondary">
            {list.groups.length > 0 ? <Trans>Assigned to: {list.groups.join(', ')}</Trans> : <Trans>Not assigned to any group yet.</Trans>}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            <Trans>Changes are live. Archived PDFs keep what they showed, and removing an item never touches review history.</Trans>
          </Typography>
        </Stack>
        <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <ButtonLink to="/admin/audit" search={{ entityId: list.id }}><Trans>History</Trans></ButtonLink>
          <Button variant="outlined" startIcon={<Plus size={20} aria-hidden />} onClick={addHeading}><Trans>Add heading</Trans></Button>
          <Button variant="outlined" onClick={addExisting}><Trans>Add existing competency</Trans></Button>
          <Button variant="contained" startIcon={<Plus size={20} aria-hidden />} onClick={newCompetency}><Trans>New competency</Trans></Button>
        </Stack>
      </Stack>
      {error && <Alert severity="error">{error}</Alert>}
    </Stack>
  )
}
