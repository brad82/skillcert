import type { PaletteMode } from '@mui/material'
import { createTheme } from '@mui/material/styles'

/**
 * Global MUI theme for a mode. Placeholder palette from the AFA paper record until the
 * Claude Design tokens land (Phase 0, task 7). Feature colour that can't come from the
 * theme goes through defineModePalette, not here.
 */
export function makeTheme(mode: PaletteMode) {
  return createTheme({
    palette: {
      mode,
      primary: { main: mode === 'light' ? '#a6192e' : '#e0566a' },
      secondary: { main: mode === 'light' ? '#2f4858' : '#8fb3c7' },
    },
    shape: { borderRadius: 8 },
  })
}
