import { useLingui } from '@lingui/react/macro'
import Tab from '@mui/material/Tab'
import Tabs from '@mui/material/Tabs'
import { useCompetencyTabs } from '../../CompetencyProvider'

const detailTabsStyles = () => ({
  tabs: { borderBottom: 1, borderColor: 'divider' },
})

/** Overview · Resources · History. */
export function DetailTabs() {
  const { tab, onTabChange } = useCompetencyTabs()
  const { t } = useLingui()
  const styles = detailTabsStyles()

  return (
    <Tabs value={tab} onChange={(_, value) => onTabChange(value)} variant="fullWidth" sx={styles.tabs}>
      <Tab value="overview" label={t`Overview`} id="tab-overview" aria-controls="tabpanel" />
      <Tab value="resources" label={t`Resources`} id="tab-resources" aria-controls="tabpanel" />
      <Tab value="history" label={t`History`} id="tab-history" aria-controls="tabpanel" />
    </Tabs>
  )
}
