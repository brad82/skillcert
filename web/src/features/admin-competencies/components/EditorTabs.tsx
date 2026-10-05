import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Tab from '@mui/material/Tab'
import Tabs from '@mui/material/Tabs'
import { visuallyHidden } from '@mui/utils'
import { tabLabels } from '../model/changeLabels'
import type { EditorTab } from '../model/useCompetencyDraft'
import { useEditorTabs } from '../CompetencyEditorProvider'

const tabs: EditorTab[] = ['details', 'resources', 'certification']

const editorTabsStyles = () => ({
  dot: { width: 8, height: 8, borderRadius: '50%', bgcolor: 'primary.main', ml: 2, display: 'inline-block' },
})

/** Details · Resources · Certification, each marked with a dot when it holds unsaved changes. */
export function EditorTabs() {
  const { tab, setTab, changed } = useEditorTabs()
  const { i18n, t } = useLingui()
  const styles = editorTabsStyles()
  return (
    <Tabs value={tab} onChange={(_, value: EditorTab) => setTab(value)} sx={{ borderBottom: 1, borderColor: 'divider' }}>
      {tabs.map((key) => (
        <Tab
          key={key}
          value={key}
          id={`tab-${key}`}
          aria-controls={`panel-${key}`}
          label={
            <span>
              {i18n._(tabLabels[key])}
              {changed(key) && (
                <>
                  <Box component="span" sx={styles.dot} aria-hidden />
                  <Box component="span" sx={visuallyHidden}>{t`(changed)`}</Box>
                </>
              )}
            </span>
          }
        />
      ))}
    </Tabs>
  )
}
