import { useLingui } from '@lingui/react/macro'
import ButtonBase from '@mui/material/ButtonBase'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { ChevronDown, ChevronRight } from 'lucide-react'
import type { ListTreeNode } from '@features/my-record'

type Props = {
  heading: ListTreeNode
  depth: number
  expanded: boolean
  onToggle: () => void
}

const headingRowStyles = (depth: number) => ({
  row: (theme: Theme) => ({
    width: '100%',
    display: 'flex',
    alignItems: 'center',
    gap: 2,
    pl: 2 + depth * 3,
    pr: 3,
    minHeight: 48,
    justifyContent: 'flex-start',
    textAlign: 'left',
    // The paper record: a solid band for sections, a tint band for sub-sections.
    bgcolor: depth === 0 ? theme.palette.primary.main : depth === 1 ? theme.palette.brandTint : 'transparent',
    color: depth === 0 ? theme.palette.primary.contrastText : 'text.primary',
    borderRadius: depth === 0 ? theme.radius.sm : 0,
  }),
  title: { flexGrow: 1, minWidth: 0, fontWeight: 600 },
  progress: { fontVariantNumeric: 'tabular-nums', opacity: 0.85, flexShrink: 0 },
})

/** A section or sub-section with "current / total" progress for its whole subtree. */
export function HeadingRow({ heading, depth, expanded, onToggle }: Props) {
  const { t } = useLingui()
  const { headingCode, headingTitle } = heading.node
  const styles = headingRowStyles(depth)

  return (
    <ButtonBase sx={styles.row} onClick={onToggle} aria-expanded={expanded}>
      {expanded ? <ChevronDown size={18} aria-hidden /> : <ChevronRight size={18} aria-hidden />}
      <Typography component="span" variant={depth === 0 ? 'h2' : 'h3'} sx={styles.title}>
        {headingCode ? `${headingCode} ` : ''}
        {headingTitle}
      </Typography>
      <Typography component="span" variant="body2" sx={styles.progress} aria-label={t`${heading.current} of ${heading.total} current`}>
        {heading.current}/{heading.total}
      </Typography>
    </ButtonBase>
  )
}
