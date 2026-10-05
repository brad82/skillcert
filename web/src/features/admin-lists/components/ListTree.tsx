import { Trans, useLingui } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useTreeRows } from '../ListEditorProvider'
import { TreeRow } from './TreeRow'

/** The list's headings and competencies in record order, collapsed headings hiding what's under them. */
export function ListTree() {
  const { rows } = useTreeRows()
  const { t } = useLingui()
  if (rows.length === 0)
    return (
      <Typography color="text.secondary">
        <Trans>This list is empty. Add a heading, or add competencies.</Trans>
      </Typography>
    )
  return (
    <Stack role="tree" aria-label={t`List contents`} spacing={1}>
      {rows.map((row) => (
        <TreeRow key={row.node.id} {...row} />
      ))}
    </Stack>
  )
}
