import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import IconButton from '@mui/material/IconButton'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { ChevronDown, ChevronRight, EllipsisVertical } from 'lucide-react'
import { useState } from 'react'
import type { AdminListNodeDto } from '@shared/api/model'
import { TextLink } from '@shared/components/RouterLinks'
import { nodeLabel } from '../model/tree'
import { useTreeRows } from '../ListEditorProvider'

type Props = {
  node: AdminListNodeDto
  collapsed: boolean
  competencyCount: number
  canMoveUp: boolean
  canMoveDown: boolean
  hasChildren: boolean
}

const treeRowStyles = (depth: number) => ({
  row: { minHeight: 48, pl: 2 + depth * 7, pr: 1, alignItems: 'center' },
  // The paper record's bands: top-level sections on brand red, sub-sections on brand tint.
  section: (theme: Theme) => ({ bgcolor: theme.palette.primary.main, color: theme.palette.primary.contrastText, borderRadius: theme.radius.sm }),
  subsection: (theme: Theme) => ({ bgcolor: theme.palette.brandTint, borderRadius: theme.radius.sm }),
  competency: { bgcolor: 'background.paper', borderBottom: 1, borderColor: 'divider' },
  code: { minWidth: 64, flexShrink: 0 },
  title: { flexGrow: 1, minWidth: 0 },
})

/** One node of the tree: a heading band (collapsible) or a competency row linking to its editor, plus its menu. */
export function TreeRow({ node, collapsed, competencyCount, canMoveUp, canMoveDown, hasChildren }: Props) {
  const { listId, busy, toggle, moveUp, moveDown, moveTo, rename, remove } = useTreeRows()
  const { t } = useLingui()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const styles = treeRowStyles(node.depth)
  const heading = node.kind === 'Heading'
  const label = nodeLabel(node)
  const act = (action: () => void) => () => {
    setAnchor(null)
    action()
  }

  return (
    <Stack
      direction="row"
      spacing={2}
      role="treeitem"
      aria-level={node.depth + 1}
      aria-expanded={heading && hasChildren ? !collapsed : undefined}
      aria-label={label}
      sx={[styles.row, heading ? (node.depth === 0 ? styles.section : styles.subsection) : styles.competency]}
    >
      {heading ? (
        <IconButton size="small" color="inherit" aria-label={collapsed ? t`Expand ${label}` : t`Collapse ${label}`} onClick={() => toggle(node.id)} disabled={!hasChildren}>
          {collapsed ? <ChevronRight size={20} aria-hidden /> : <ChevronDown size={20} aria-hidden />}
        </IconButton>
      ) : null}
      <Typography variant="code" sx={styles.code}>
        {heading ? node.headingCode : node.competency?.code}
      </Typography>
      {heading ? (
        <Typography variant={node.depth === 0 ? 'h2' : 'h3'} component="span" sx={styles.title}>
          {node.headingTitle}
          {collapsed && <Typography component="span" variant="body2"> · <Trans>{competencyCount} competencies</Trans></Typography>}
        </Typography>
      ) : (
        <TextLink
          to="/admin/competencies/$competencyId"
          params={{ competencyId: node.competency!.id }}
          search={{ listId }}
          color="inherit"
          sx={styles.title}
        >
          {node.competency?.title}
        </TextLink>
      )}
      {node.competency && node.competency.listCount > 1 && (
        <Chip size="small" variant="outlined" label={<Trans>In {node.competency.listCount} lists</Trans>} />
      )}
      {node.competency && !node.competency.isActive && <Chip size="small" variant="outlined" label={<Trans>Inactive</Trans>} />}
      <IconButton color="inherit" aria-label={t`Actions for ${label}`} disabled={busy} onClick={(e) => setAnchor(e.currentTarget)}>
        <EllipsisVertical size={20} aria-hidden />
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={() => setAnchor(null)}>
        {heading && <MenuItem onClick={act(() => rename(node.id))}><Trans>Rename</Trans></MenuItem>}
        <MenuItem disabled={!canMoveUp} onClick={act(() => moveUp(node.id))}><Trans>Move up</Trans></MenuItem>
        <MenuItem disabled={!canMoveDown} onClick={act(() => moveDown(node.id))}><Trans>Move down</Trans></MenuItem>
        <MenuItem onClick={act(() => moveTo(node.id))}><Trans>Move to…</Trans></MenuItem>
        <MenuItem onClick={act(() => remove(node.id))}><Trans>Remove from list</Trans></MenuItem>
      </Menu>
    </Stack>
  )
}
