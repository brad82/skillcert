import { useLingui } from '@lingui/react/macro'
import IconButton from '@mui/material/IconButton'
import { Moon, Sun } from 'lucide-react'
import { useThemeMode } from '@shared/lib/ThemeModeProvider'

export function ThemeModeToggle() {
  const { mode, toggleMode } = useThemeMode()
  const { t } = useLingui()

  return (
    <IconButton
      color="inherit"
      onClick={toggleMode}
      aria-label={mode === 'light' ? t`Switch to dark mode` : t`Switch to light mode`}
    >
      {mode === 'light' ? <Moon size={20} aria-hidden /> : <Sun size={20} aria-hidden />}
    </IconButton>
  )
}
