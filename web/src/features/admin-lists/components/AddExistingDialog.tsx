import { Trans, useLingui } from '@lingui/react/macro'
import Checkbox from '@mui/material/Checkbox'
import List from '@mui/material/List'
import ListItem from '@mui/material/ListItem'
import ListItemButton from '@mui/material/ListItemButton'
import ListItemIcon from '@mui/material/ListItemIcon'
import ListItemText from '@mui/material/ListItemText'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useAddExistingDialog } from '../ListEditorProvider'
import { EditorDialog } from './EditorDialog'
import { ParentPicker } from './ParentPicker'

/**
 * Shares existing competencies into this list. Each result says where it is already used; ones already in
 * this list are greyed out (a competency appears at most once per list).
 */
export function AddExistingDialog() {
  const { open, listTitle, draft, parents, results, count, busy, error, close, submit } = useAddExistingDialog()
  const { t } = useLingui()
  return (
    <EditorDialog
      open={open}
      title={<Trans>Add existing competencies to {listTitle}</Trans>}
      subtitle={<Trans>Adding a competency shares it: edits to it apply in every list that uses it.</Trans>}
      error={error}
      submitLabel={count === 1 ? <Trans>Add 1 competency</Trans> : <Trans>Add {count} competencies</Trans>}
      canSubmit={count > 0}
      busy={busy}
      onClose={close}
      onSubmit={() => void submit()}
      footerNote={<Trans>{count} selected</Trans>}
    >
      <ParentPicker label={t`Under heading`} value={draft.parentId} onChange={draft.setParentId} headings={parents} />
      <TextField label={t`Search all competencies`} type="search" value={draft.search} onChange={(e) => draft.setSearch(e.target.value)} autoFocus />
      <List dense aria-label={t`Results`} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, maxHeight: 320, overflowY: 'auto' }}>
        {results.map(({ competency, inList }) => (
          <ListItem key={competency.id} disablePadding>
            <ListItemButton disabled={inList} onClick={() => draft.toggle(competency.id)}>
              <ListItemIcon>
                <Checkbox edge="start" tabIndex={-1} disableRipple checked={inList || draft.selected.has(competency.id)} slotProps={{ input: { 'aria-label': `${competency.code} ${competency.title}` } }} />
              </ListItemIcon>
              <ListItemText
                primary={<><Typography variant="code" sx={{ mr: 2 }}>{competency.code}</Typography>{competency.title}</>}
                secondary={
                  inList ? <Trans>Already in this list</Trans> : competency.listCount === 0 ? <Trans>Not in any list</Trans> : <Trans>In {competency.listCount} lists</Trans>
                }
              />
            </ListItemButton>
          </ListItem>
        ))}
        {results.length === 0 && (
          <ListItem><Typography color="text.secondary"><Trans>No competencies match.</Trans></Typography></ListItem>
        )}
      </List>
    </EditorDialog>
  )
}
