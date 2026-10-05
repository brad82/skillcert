import { Trans, useLingui } from '@lingui/react/macro'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { reviewerLabel } from '@shared/lib/reviewers'

type Props = { codes: string[] }

/** The reviewer classifications a user holds, as outlined chips; "None" when they hold none. */
export function ClassificationChips({ codes }: Props) {
  const { i18n } = useLingui()
  if (codes.length === 0)
    return (
      <Typography variant="body2" color="text.secondary">
        <Trans>None</Trans>
      </Typography>
    )
  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
      {codes.map((code) => (
        <Chip key={code} size="small" variant="outlined" label={i18n._(reviewerLabel('Classified', code))} />
      ))}
    </Stack>
  )
}
