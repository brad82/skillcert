import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import ButtonBase from '@mui/material/ButtonBase'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { createLink } from '@tanstack/react-router'
import type { StateCounts } from '@features/my-record'
import type { PresentationState } from '@shared/lib/competencyStatus'
import type { CompetencyStatusKey } from '@shared/lib/theme'

type Props = {
  counts: StateCounts
}

type TileState = Extract<PresentationState, 'expired' | 'expiringSoon' | 'pending' | 'notCertified'>

/** A MUI ButtonBase that navigates with the router's typed `to`/`search`. */
const TileLink = createLink(ButtonBase)

const tiles: { state: TileState; colours: CompetencyStatusKey; label: MessageDescriptor }[] = [
  { state: 'expired', colours: 'expired', label: msg`Expired` },
  { state: 'expiringSoon', colours: 'expired', label: msg`Expiring ≤ 30 days` },
  { state: 'pending', colours: 'pending', label: msg`Pending` },
  { state: 'notCertified', colours: 'notCertified', label: msg`Not certified` },
]

const statusTilesStyles = () => ({
  grid: { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 },
  tile: (colours: CompetencyStatusKey) => (theme: Theme) => ({
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-start',
    p: 3,
    minHeight: 80,
    borderRadius: theme.radius.md,
    bgcolor: theme.palette.status[colours].bg,
    color: theme.palette.status[colours].fg,
    textAlign: 'left',
  }),
  count: { fontSize: 28, lineHeight: 1, fontWeight: 700, fontVariantNumeric: 'tabular-nums' },
})

/** Four counts; each tile opens the skills list filtered to that state (wireframe 1b). */
export function StatusTiles({ counts }: Props) {
  const { i18n } = useLingui()
  const styles = statusTilesStyles()

  return (
    <Box sx={styles.grid}>
      {tiles.map(({ state, colours, label }) => (
        <TileLink key={state} to="/skills" search={{ status: state }} sx={styles.tile(colours)}>
          <Typography component="span" sx={styles.count}>
            {counts[state]}
          </Typography>
          <Typography component="span" variant="body2" sx={{ color: 'inherit', fontWeight: 600 }}>
            {i18n._(label)}
          </Typography>
        </TileLink>
      ))}
    </Box>
  )
}
