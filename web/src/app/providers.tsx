import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClientProvider } from '@tanstack/react-query'
import { type ReactNode, useMemo } from 'react'
import { AppI18nProvider } from '@shared/i18n/AppI18nProvider'
import { makeTheme } from '@shared/lib/theme'
import { ThemeModeProvider, useThemeMode } from '@shared/lib/ThemeModeProvider'
import { queryClient } from './queryClient'

function AppThemeProvider({ children }: { children: ReactNode }) {
  const { mode } = useThemeMode()
  const theme = useMemo(() => makeTheme(mode), [mode])
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  )
}

/** Composition root: i18n › server-state cache › theme mode › MUI theme. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <AppI18nProvider>
      <QueryClientProvider client={queryClient}>
        <ThemeModeProvider>
          <AppThemeProvider>{children}</AppThemeProvider>
        </ThemeModeProvider>
      </QueryClientProvider>
    </AppI18nProvider>
  )
}
