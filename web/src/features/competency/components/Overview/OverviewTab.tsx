import { Trans, useLingui } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import type { Theme } from '@mui/material/styles'
import Typography from '@mui/material/Typography'
import { reviewLevelLabel } from '@shared/lib/reviewers'
import { useOverview } from '../../CompetencyProvider'
import { KeyFacts } from './KeyFacts'
import { PendingNotice } from './PendingNotice'

const overviewTabStyles = () => ({
  label: (theme: Theme) => ({ ...theme.typography.overline, color: 'text.secondary', display: 'block', mb: 1 }),
})

/** Wireframe 3c: full title, pending notice, key facts, who can sign, description, where it sits. */
export function OverviewTab() {
  const { title, description, lowestReviewer, partOf } = useOverview()
  const { i18n } = useLingui()
  const styles = overviewTabStyles()
  const lowest = i18n._(reviewLevelLabel(lowestReviewer))

  return (
    <Stack spacing={6}>
      <Typography variant="h3">{title}</Typography>
      <PendingNotice />
      <KeyFacts />
      <Typography>
        <Trans>
          Can be signed by: <strong>{lowest} or higher</strong>
        </Trans>
      </Typography>
      {description && (
        <div>
          <Typography component="span" sx={styles.label}>
            <Trans>Description</Trans>
          </Typography>
          <Typography>{description}</Typography>
        </div>
      )}
      {partOf.length > 0 && (
        <div>
          <Typography component="span" sx={styles.label}>
            <Trans>Part of</Trans>
          </Typography>
          {partOf.map((path) => (
            <Typography key={path.listId} variant="body2" color="text.secondary">
              {[path.listTitle, ...path.headings].join(' › ')}
            </Typography>
          ))}
        </div>
      )}
    </Stack>
  )
}
