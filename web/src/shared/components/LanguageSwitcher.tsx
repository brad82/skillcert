import { useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import { Languages } from 'lucide-react'
import { useLocale } from '@shared/i18n/AppI18nProvider'

const languageSwitcherStyles = () => ({
  button: { minWidth: 0, gap: 1, color: 'inherit' },
})

/** Toggles English ⇄ French. Labels name the target language in that language. */
export function LanguageSwitcher() {
  const { locale, setLocale } = useLocale()
  const { t } = useLingui()
  const styles = languageSwitcherStyles()
  const next = locale === 'en' ? 'fr' : 'en'

  return (
    <Button
      sx={styles.button}
      onClick={() => setLocale(next)}
      aria-label={t`Change language`}
      lang={next}
    >
      <Languages size={18} aria-hidden />
      {next === 'fr' ? 'Français' : 'English'}
    </Button>
  )
}
