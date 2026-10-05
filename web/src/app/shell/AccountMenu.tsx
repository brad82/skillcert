import { msg } from '@lingui/core/macro'
import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import Divider from '@mui/material/Divider'
import IconButton from '@mui/material/IconButton'
import ListItemIcon from '@mui/material/ListItemIcon'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { CircleUser, Languages, LogOut, Moon, Sun } from 'lucide-react'
import { useState } from 'react'
import { type Capability, useCurrentUser } from '@features/current-user'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { useThemeMode } from '@shared/lib/ThemeModeProvider'

type Props = {
  signingOut: boolean
  onSignOut: () => void
}

const capabilityLabels = { Administrator: msg`Administrator`, Instructor: msg`Instructor`, Supervisor: msg`Supervisor` }

/** Who is signed in, what they can do, and the per-device settings: language, theme, sign out. */
export function AccountMenu({ signingOut, onSignOut }: Props) {
  const { displayName, email, capabilities } = useCurrentUser()
  const { locale, setLocale } = useLocale()
  const { mode, toggleMode } = useThemeMode()
  const { i18n, t } = useLingui()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const close = () => setAnchor(null)

  return (
    <>
      <IconButton color="inherit" aria-label={t`Account`} onClick={(event) => setAnchor(event.currentTarget)}>
        <CircleUser size={24} aria-hidden />
      </IconButton>
      <Menu anchorEl={anchor} open={anchor !== null} onClose={close} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
        <Stack sx={{ px: 4, py: 2, maxWidth: 280 }} spacing={1}>
          <Typography variant="body1" sx={{ fontWeight: 600 }}>
            {displayName}
          </Typography>
          <Typography variant="body2" color="text.secondary" noWrap>
            {email}
          </Typography>
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', pt: 1 }}>
            <Chip size="small" label={<Trans>Candidate</Trans>} />
            {capabilities.map((capability) => {
              const label = capabilityLabels[capability as Capability]
              return <Chip key={capability} size="small" color="primary" label={label ? i18n._(label) : capability} />
            })}
          </Stack>
        </Stack>
        <Divider />
        <MenuItem lang={locale === 'en' ? 'fr' : 'en'} onClick={() => { setLocale(locale === 'en' ? 'fr' : 'en'); close() }}>
          <ListItemIcon><Languages size={18} aria-hidden /></ListItemIcon>
          {locale === 'en' ? 'Français' : 'English'}
        </MenuItem>
        <MenuItem onClick={() => { toggleMode(); close() }}>
          <ListItemIcon>{mode === 'light' ? <Moon size={18} aria-hidden /> : <Sun size={18} aria-hidden />}</ListItemIcon>
          {mode === 'light' ? <Trans>Dark mode</Trans> : <Trans>Light mode</Trans>}
        </MenuItem>
        <MenuItem disabled={signingOut} onClick={() => { close(); onSignOut() }}>
          <ListItemIcon><LogOut size={18} aria-hidden /></ListItemIcon>
          <Trans>Sign out</Trans>
        </MenuItem>
      </Menu>
    </>
  )
}
