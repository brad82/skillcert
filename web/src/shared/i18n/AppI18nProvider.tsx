import { I18nProvider } from '@lingui/react'
import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { activateLocale, detectLocale, i18n, type Locale, persistLocale } from './runtime'

export type LocaleState = {
  locale: Locale
  setLocale: (locale: Locale) => void
}

const LocaleContext = createContext<LocaleState | null>(null)

/**
 * Owns the active locale: detects it, loads its catalogs, keeps <html lang> in sync and
 * persists an explicit choice. Renders nothing until the first catalog is loaded, so no
 * untranslated flash. Does not translate MUI's own strings or API error messages.
 */
export function AppI18nProvider({ children, initialLocale }: { children: ReactNode; initialLocale?: Locale }) {
  const [locale, setLocaleState] = useState<Locale>(() => initialLocale ?? detectLocale())
  const [loadedLocale, setLoadedLocale] = useState<Locale | null>(null)

  useEffect(() => {
    let cancelled = false
    void activateLocale(locale).then(() => {
      if (!cancelled) setLoadedLocale(locale)
    })
    return () => {
      cancelled = true
    }
  }, [locale])

  const setLocale = useCallback((next: Locale) => {
    persistLocale(next)
    setLocaleState(next)
  }, [])

  const value = useMemo(() => ({ locale, setLocale }), [locale, setLocale])

  if (!loadedLocale) return null

  return (
    <LocaleContext.Provider value={value}>
      <I18nProvider i18n={i18n}>{children}</I18nProvider>
    </LocaleContext.Provider>
  )
}

export function useLocale(): LocaleState {
  const context = useContext(LocaleContext)
  if (!context) throw new Error('useLocale must be used within an AppI18nProvider')
  return context
}
