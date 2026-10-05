import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import { CertificationTab } from '../components/CertificationTab'
import { CompetencyHeader } from '../components/CompetencyHeader'
import { DeactivateDialog } from '../components/DeactivateDialog'
import { DetailsTab } from '../components/DetailsTab'
import { EditorFooter } from '../components/EditorFooter'
import { EditorTabs } from '../components/EditorTabs'
import { ResourcesTab } from '../components/ResourcesTab'
import { ReviewDialog } from '../components/ReviewDialog'
import { RevisionHistory } from '../components/RevisionHistory'
import { useEditorTabs } from '../CompetencyEditorProvider'

/** Competency editor: header, tabbed form with "Publish changes", history beside it. Layout only. */
export function CompetencyEditorPage() {
  const { tab } = useEditorTabs()
  return (
    <Stack spacing={6}>
      <CompetencyHeader />
      <Stack direction="row" spacing={6} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'flex-start' }}>
        <Paper variant="outlined" sx={{ p: 6, flex: '999 1 520px', minWidth: 0 }}>
          <Stack spacing={6}>
            <EditorTabs />
            {tab === 'details' && <DetailsTab />}
            {tab === 'resources' && <ResourcesTab />}
            {tab === 'certification' && <CertificationTab />}
            <EditorFooter />
          </Stack>
        </Paper>
        <RevisionHistory />
      </Stack>
      <ReviewDialog />
      <DeactivateDialog />
    </Stack>
  )
}
