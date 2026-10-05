import { Trans } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useEmptySignOff } from '../SignOffProvider'

/** The skills to sign aren't in the basket any more (e.g. it was cleared in another tab). */
export function EmptySignOff() {
  const { onDone } = useEmptySignOff()
  return (
    <Stack spacing={3} sx={{ alignItems: 'center', textAlign: 'center', pt: 12 }}>
      <Typography>
        <Trans>There's nothing to sign off. Add skills to your basket first.</Trans>
      </Typography>
      <Button variant="outlined" onClick={onDone}>
        <Trans>Back to basket</Trans>
      </Button>
    </Stack>
  )
}
