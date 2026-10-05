import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { useLingui } from '@lingui/react/macro'
import Badge from '@mui/material/Badge'
import BottomNavigation from '@mui/material/BottomNavigation'
import BottomNavigationAction from '@mui/material/BottomNavigationAction'
import Paper from '@mui/material/Paper'
import { Link, useRouterState } from '@tanstack/react-router'
import { CalendarDays, FileText, House, ListChecks, ShoppingBasket, type LucideIcon } from 'lucide-react'
import { useBasket } from '@features/basket'

const tabs: { to: '/' | '/skills' | '/basket' | '/events' | '/records'; label: MessageDescriptor; icon: LucideIcon }[] = [
  { to: '/', label: msg`Home`, icon: House },
  { to: '/skills', label: msg`Skills`, icon: ListChecks },
  { to: '/basket', label: msg`Basket`, icon: ShoppingBasket },
  { to: '/events', label: msg`Events`, icon: CalendarDays },
  { to: '/records', label: msg`Records`, icon: FileText },
]

const bottomNavStyles = () => ({
  bar: {
    position: 'fixed',
    left: 0,
    right: 0,
    bottom: 0,
    pb: 'env(safe-area-inset-bottom, 0px)',
    borderTop: 1,
    borderColor: 'divider',
    zIndex: 'appBar',
  },
  action: { minWidth: 0, px: 1 },
})

/** The candidate app's five tabs (docs/candidate-app.md). The basket tab carries the basket count. */
export function BottomNav() {
  const { i18n } = useLingui()
  const { count } = useBasket()
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const active = tabs.findLast((tab) => (tab.to === '/' ? pathname === '/' : pathname.startsWith(tab.to)))?.to ?? false
  const styles = bottomNavStyles()

  return (
    <Paper component="nav" square elevation={0} sx={styles.bar}>
      <BottomNavigation showLabels value={active}>
        {tabs.map(({ to, label, icon: Icon }) => (
          <BottomNavigationAction
            key={to}
            value={to}
            label={i18n._(label)}
            component={Link}
            to={to}
            sx={styles.action}
            icon={
              to === '/basket' ? (
                <Badge badgeContent={count} color="primary">
                  <Icon size={22} aria-hidden />
                </Badge>
              ) : (
                <Icon size={22} aria-hidden />
              )
            }
          />
        ))}
      </BottomNavigation>
    </Paper>
  )
}
