import Box from '@mui/material/Box'
import Stack from '@mui/material/Stack'
import { BasketAction } from '../components/BasketAction'
import { DetailHeader } from '../components/Header/DetailHeader'
import { DetailTabs } from '../components/Header/DetailTabs'
import { HistoryTab } from '../components/History/HistoryTab'
import { ReviewSheet } from '../components/History/ReviewSheet'
import { OverviewTab } from '../components/Overview/OverviewTab'
import { ResourcesTab } from '../components/Resources/ResourcesTab'
import { useCompetencyTabs } from '../CompetencyProvider'

const panels = { overview: OverviewTab, resources: ResourcesTab, history: HistoryTab }

/** Wireframe 3b: header and status, tabs, the active tab, and the basket action. Layout only. */
export function CompetencyPage() {
  const { tab } = useCompetencyTabs()
  const Panel = panels[tab]

  return (
    <Stack spacing={4} sx={{ pb: 20 }}>
      <DetailHeader />
      <DetailTabs />
      <Box role="tabpanel" id="tabpanel" aria-labelledby={`tab-${tab}`}>
        <Panel />
      </Box>
      <BasketAction />
      <ReviewSheet />
    </Stack>
  )
}
