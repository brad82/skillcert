import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import List from '@mui/material/List'
import ListItemText from '@mui/material/ListItemText'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useRouterState } from '@tanstack/react-router'
import { ListItemLink } from '@shared/components/RouterLinks'
import { ChevronDown, ChevronRight } from 'lucide-react'

type Section = '/admin/users' | '/admin/lists' | '/admin/audit'
type SubSection = '/admin/competencies' | '/admin/import'

const later: { label: MessageDescriptor; phase: string; after: Section | null }[] = [
  { label: msg`Dashboard`, phase: '5', after: null },
  { label: msg`Groups`, phase: '6', after: '/admin/users' },
  { label: msg`Opportunities`, phase: '6', after: '/admin/lists' },
]

const sections: { to: Section; label: MessageDescriptor }[] = [
  { to: '/admin/users', label: msg`Users` },
  { to: '/admin/lists', label: msg`Competency lists` },
  { to: '/admin/audit', label: msg`Audit log` },
]

/** Shown under Competency lists, only while the admin is in that area. */
const listChildren: { to: SubSection; label: MessageDescriptor }[] = [
  { to: '/admin/competencies', label: msg`All competencies` },
  { to: '/admin/import', label: msg`CSV import` },
]

const listArea = ['/admin/lists', '/admin/competencies', '/admin/import']

const adminNavStyles = () => ({
  brand: { bgcolor: 'primary.main', color: 'primary.contrastText', px: 5, minHeight: 56, alignItems: 'center' },
  list: { px: 3, py: 4 },
  item: { borderRadius: 1, minHeight: 44, '&.active': { bgcolor: 'brandTint', fontWeight: 600 } },
  child: { pl: 8 },
  later: { minHeight: 44, px: 4, color: 'text.secondary', justifyContent: 'space-between', alignItems: 'center' },
})

type Props = {
  /** Closes the temporary drawer on phones after choosing a page. */
  onNavigate?: () => void
}

/**
 * The admin menu's contents, shared by the docked drawer (desktop) and the temporary drawer (phones).
 * Later-phase sections show as disabled rows so the shape of the admin area is visible.
 */
export function AdminNav({ onNavigate }: Props) {
  const { i18n } = useLingui()
  const pathname = useRouterState({ select: (s) => s.location.pathname })
  const inListArea = listArea.some((path) => pathname.startsWith(path))
  const styles = adminNavStyles()

  const laterRow = (after: Section | null) =>
    later
      .filter((item) => item.after === after)
      .map((item) => (
        <Stack key={item.phase + item.label.id} direction="row" sx={styles.later}>
          <Typography variant="body2">{i18n._(item.label)}</Typography>
          <Chip size="small" variant="outlined" label={<Trans>Phase {item.phase}</Trans>} />
        </Stack>
      ))

  return (
    <>
      <Stack direction="row" spacing={2} sx={styles.brand}>
        <Typography variant="h3" component="span">
          SkillCert
        </Typography>
        <Typography variant="overline">
          <Trans>Administration</Trans>
        </Typography>
      </Stack>
      <List component="nav" aria-label={i18n._(msg`Admin sections`)} sx={styles.list}>
        {laterRow(null)}
        {sections.map(({ to, label }) => (
          <Stack key={to}>
            <ListItemLink to={to} onClick={onNavigate} sx={styles.item}>
              <ListItemText primary={i18n._(label)} />
              {to === '/admin/lists' && (inListArea ? <ChevronDown size={16} aria-hidden /> : <ChevronRight size={16} aria-hidden />)}
            </ListItemLink>
            {to === '/admin/lists' &&
              inListArea &&
              listChildren.map((child) => (
                <ListItemLink key={child.to} to={child.to} onClick={onNavigate} sx={[styles.item, styles.child]}>
                  <ListItemText primary={i18n._(child.label)} />
                </ListItemLink>
              ))}
            {laterRow(to)}
          </Stack>
        ))}
      </List>
    </>
  )
}
