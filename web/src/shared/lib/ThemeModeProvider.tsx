import type { PaletteMode } from '@mui/material'
import { createContext, type ReactNode, useCallback, useContext, useMemo, useState } from 'react'

const storageKey = 'themeMode'

export type ThemeModeState = {
  mode: PaletteMode
  toggleMode: () => void
}

const ThemeModeContext = createContext<ThemeModeState | null>(null)

function initialMode(): PaletteMode {
  const stored = localStorage.getItem(storageKey)
  if (stored === 'light' || stored === 'dark') return stored
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

/**
 * Owns the light/dark choice: stored choice, else the OS preference. Client-only, like locale.
 * Context rather than prop-drilling because the router sits between App and the header toggle.
 * Knows nothing about the MUI theme itself — see makeTheme.
 */
export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<PaletteMode>(initialMode)

  const toggleMode = useCallback(() => {
    setMode((current) => {
      const next = current === 'light' ? 'dark' : 'light'
      localStorage.setItem(storageKey, next)
      return next
    })
  }, [])

  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode])
  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>
}

export function useThemeMode(): ThemeModeState {
  const context = useContext(ThemeModeContext)
  if (!context) throw new Error('useThemeMode must be used within a ThemeModeProvider')
  return context
}
