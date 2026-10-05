import type { Theme } from '@mui/material/styles'
import { useTheme } from '@mui/material/styles'

type ModeTables<T> = { light: T; dark: T }

/**
 * Turns two flat { light, dark } colour tables (or a theme => tables factory, for values that
 * must read the MUI theme) into a hook returning the table for the active mode. Replaces
 * `isDark ? … : …` ternaries. Prefer theme tokens ("text.secondary", "divider") for surfaces
 * that are actually themed; use this only for colour the theme can't express.
 */
export function defineModePalette<T>(definition: ModeTables<T> | ((theme: Theme) => ModeTables<T>)) {
  return function usePalette(): T {
    const theme = useTheme()
    const tables = typeof definition === 'function' ? definition(theme) : definition
    return tables[theme.palette.mode]
  }
}
