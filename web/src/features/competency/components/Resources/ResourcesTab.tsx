import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import { Trans, useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { ExternalLink, FileText, Globe, Play, type LucideIcon } from 'lucide-react'
import type { ResourceType } from '@shared/api/model'
import { useResourceGroups } from '../../CompetencyProvider'

const groupAppearance: Record<ResourceType, { label: MessageDescriptor; icon: LucideIcon }> = {
  Video: { label: msg`Video`, icon: Play },
  WebPage: { label: msg`Web page`, icon: Globe },
  Document: { label: msg`Document`, icon: FileText },
}

const resourcesTabStyles = () => ({
  heading: (theme: Theme) => ({ ...theme.typography.overline, color: 'text.secondary' }),
  item: (theme: Theme) => ({
    display: 'flex',
    alignItems: 'center',
    gap: 3,
    p: 3,
    border: 1,
    borderColor: 'divider',
    borderRadius: theme.radius.md,
    color: 'text.primary',
    textDecoration: 'none',
    '&:hover': { borderColor: theme.palette.lineStrong },
  }),
  thumb: (theme: Theme) => ({
    width: 72,
    height: 48,
    flexShrink: 0,
    display: 'grid',
    placeItems: 'center',
    borderRadius: theme.radius.sm,
    bgcolor: theme.palette.brandTint,
    color: 'primary.main',
  }),
  icon: { display: 'grid', placeItems: 'center', color: 'text.secondary', flexShrink: 0 },
  title: { flexGrow: 1, minWidth: 0 },
})

/** Wireframe 3d: resources grouped by type; every link opens outside SkillCert. */
export function ResourcesTab() {
  const groups = useResourceGroups()
  const { i18n, t } = useLingui()
  const styles = resourcesTabStyles()

  if (groups.length === 0) {
    return (
      <Typography color="text.secondary">
        <Trans>No resources for this skill.</Trans>
      </Typography>
    )
  }

  return (
    <Stack spacing={6}>
      {groups.map(({ type, resources }) => {
        const { label, icon: Icon } = groupAppearance[type]
        return (
          <Stack key={type} spacing={2} component="section" aria-label={i18n._(label)}>
            <Typography component="h2" sx={styles.heading}>
              {i18n._(label)}
            </Typography>
            {resources.map((resource) => (
              <Link
                key={resource.url}
                href={resource.url}
                target="_blank"
                rel="noopener noreferrer"
                aria-label={t`${resource.title} (opens in a new tab)`}
                sx={styles.item}
              >
                <Box sx={type === 'Video' ? styles.thumb : styles.icon}>
                  <Icon size={type === 'Video' ? 24 : 20} aria-hidden />
                </Box>
                <Typography component="span" sx={styles.title}>
                  {resource.title}
                </Typography>
                <ExternalLink size={16} aria-hidden />
              </Link>
            ))}
          </Stack>
        )
      })}
    </Stack>
  )
}
