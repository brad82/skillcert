import Stack from '@mui/material/Stack'
import { AddExistingDialog } from '../components/AddExistingDialog'
import { HeadingDialog } from '../components/HeadingDialog'
import { ListEditorHeader } from '../components/ListEditorHeader'
import { ListTree } from '../components/ListTree'
import { MoveDialog } from '../components/MoveDialog'
import { NewCompetencyDialog } from '../components/NewCompetencyDialog'
import { RemoveNodeDialog } from '../components/RemoveNodeDialog'
import { RenameListDialog } from '../components/RenameListDialog'

/** The list tree editor: header and toolbar, the tree, and its dialogs. Layout only. */
export function ListEditorPage() {
  return (
    <Stack spacing={6}>
      <ListEditorHeader />
      <ListTree />
      <RenameListDialog />
      <HeadingDialog />
      <MoveDialog />
      <RemoveNodeDialog />
      <AddExistingDialog />
      <NewCompetencyDialog />
    </Stack>
  )
}
