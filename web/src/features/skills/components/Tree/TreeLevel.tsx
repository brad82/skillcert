import Box from '@mui/material/Box'
import Collapse from '@mui/material/Collapse'
import type { Expansion } from '../../model/useExpansion'
import type { VisibleNode } from '../../model/skillsTree'
import { CompetencyRow } from './CompetencyRow'
import { HeadingRow } from './HeadingRow'

type Props = {
  nodes: VisibleNode[]
  depth: number
  expansion: Expansion
}

/** Renders one level of the visible tree, recursing into open headings. */
export function TreeLevel({ nodes, depth, expansion }: Props) {
  return (
    <Box role={depth === 0 ? undefined : 'group'}>
      {nodes.map(({ source, children }) => {
        const competency = source.node.competency
        if (competency) return <CompetencyRow key={source.node.id} competency={competency} depth={depth} />
        const expanded = expansion.isExpanded(source.node.id)
        return (
          <Box key={source.node.id} sx={{ mt: depth === 0 ? 2 : 0 }}>
            <HeadingRow heading={source} depth={depth} expanded={expanded} onToggle={() => expansion.toggle(source.node.id)} />
            <Collapse in={expanded} unmountOnExit>
              <TreeLevel nodes={children} depth={depth + 1} expansion={expansion} />
            </Collapse>
          </Box>
        )
      })}
    </Box>
  )
}
