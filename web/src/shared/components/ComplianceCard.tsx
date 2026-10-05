import { Plural, Trans } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import LinearProgress from '@mui/material/LinearProgress'
import type { Theme } from '@mui/material/styles'
import type { ReactNode } from 'react'
import Typography from '@mui/material/Typography'
import { CircleCheck, TriangleAlert } from 'lucide-react'
import type { MyListDto } from '@shared/api/model'

type Props = {
  list: MyListDto
  /** Shown under the progress bar, e.g. the Records tab's download button. */
  action?: ReactNode
}

const complianceCardStyles = (compliant: boolean) => ({
  header: { display: 'flex', alignItems: 'center', gap: 3 },
  title: { flexGrow: 1, minWidth: 0 },
  mark: (theme: Theme) => ({
    display: 'grid',
    placeItems: 'center',
    width: 36,
    height: 36,
    borderRadius: theme.radius.pill,
    bgcolor: compliant ? theme.palette.status.current.bg : theme.palette.status.expired.bg,
    color: compliant ? theme.palette.status.current.fg : theme.palette.status.expired.fg,
    flexShrink: 0,
  }),
  progress: { mt: 3, height: 8, borderRadius: 999 },
  action: { mt: 4 },
})

/** One required list (Home 1b, Records 7a): compliant or how many to go (spec §21: compliant only when every competency is Current). */
export function ComplianceCard({ list, action }: Props) {
  const compliant = list.isCompliant
  const toGo = list.counts.total - list.counts.current
  const styles = complianceCardStyles(compliant)

  return (
    <Card>
      <CardContent>
        <Box sx={styles.header}>
          <Box sx={styles.title}>
            <Typography variant="h3" component="h2">
              {list.title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {compliant ? (
                <Trans>Compliant · all {list.counts.total} current</Trans>
              ) : (
                <Trans>
                  Not compliant · <Plural value={toGo} one="# to go" other="# to go" />
                </Trans>
              )}
            </Typography>
          </Box>
          <Box sx={styles.mark}>{compliant ? <CircleCheck size={20} aria-hidden /> : <TriangleAlert size={20} aria-hidden />}</Box>
        </Box>
        <LinearProgress
          variant="determinate"
          value={list.counts.total === 0 ? 0 : (list.counts.current / list.counts.total) * 100}
          sx={styles.progress}
          aria-label={list.title}
        />
        {action && <Box sx={styles.action}>{action}</Box>}
      </CardContent>
    </Card>
  )
}
