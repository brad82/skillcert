import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { AppI18nProvider } from '@shared/i18n/AppI18nProvider'
import type { Locale } from '@shared/i18n/runtime'
import { makeTheme } from '@shared/lib/theme'
import { ThemeModeProvider } from '@shared/lib/ThemeModeProvider'

/** The real i18n and theme providers, for page tests. Data-layer providers are left to the test. */
export function TestProviders({ children, locale = 'en' }: { children: ReactNode; locale?: Locale }) {
  return (
    <AppI18nProvider initialLocale={locale}>
      <ThemeModeProvider>
        <ThemeProvider theme={makeTheme('light')}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </ThemeModeProvider>
    </AppI18nProvider>
  )
}
