import { Trans } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import { type ListView, useListSection } from '../SkillsProvider'
import { TreeLevel } from './Tree/TreeLevel'

type Props = {
  view: ListView
  showTitle: boolean
}

/** One required list's tree. The title shows only when the candidate has several lists. */
export function ListSection({ view, showTitle }: Props) {
  const { visible, expansion } = useListSection(view)

  return (
    <Box component="section" aria-label={view.list.title}>
      {showTitle ? (
        <Typography variant="h2" sx={{ mb: 2 }}>
          {view.list.title}
        </Typography>
      ) : null}
      {visible.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 6, textAlign: 'center' }}>
          <Trans>No skills match.</Trans>
        </Typography>
      ) : (
        <TreeLevel nodes={visible} depth={0} expansion={expansion} />
      )}
    </Box>
  )
}
