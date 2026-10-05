import { i18n, type Messages } from '@lingui/core'

export const locales = ['en', 'fr'] as const
export type Locale = (typeof locales)[number]
export const sourceLocale: Locale = 'en'

const storageKey = 'locale'

// Every feature/app/shared catalog. A new feature's locales/*.po is picked up with no wiring.
const catalogs = import.meta.glob<{ messages: Messages }>('/src/**/locales/*.po')

const isLocale = (value: string | null | undefined): value is Locale =>
  locales.includes(value as Locale)

/** Stored explicit choice → browser language → English. */
export function detectLocale(): Locale {
  const stored = localStorage.getItem(storageKey)
  if (isLocale(stored)) return stored
  const browser = navigator.language?.slice(0, 2)
  return isLocale(browser) ? browser : sourceLocale
}

export function persistLocale(locale: Locale) {
  localStorage.setItem(storageKey, locale)
}

/** Loads and merges every catalog for the locale, then activates it on the global i18n instance. */
export async function activateLocale(locale: Locale) {
  const loaders = Object.entries(catalogs)
    .filter(([path]) => path.endsWith(`/locales/${locale}.po`))
    .map(([, load]) => load())
  const modules = await Promise.all(loaders)
  i18n.load(locale, Object.assign({}, ...modules.map((module) => module.messages)))
  i18n.activate(locale)
  document.documentElement.lang = locale
}

export { i18n }
