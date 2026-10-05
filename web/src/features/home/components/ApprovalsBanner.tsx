import { Plural, Trans } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import { Stamp } from 'lucide-react'

type Props = {
  count: number
  onOpen: () => void
}

/** Shown to anyone named as reviewer on claims still waiting for them, e.g. a supervisor. */
export function ApprovalsBanner({ count, onOpen }: Props) {
  if (count === 0) return null
  return (
    <Alert
      severity="info"
      icon={<Stamp size={20} aria-hidden />}
      action={
        <Button color="inherit" size="small" onClick={onOpen}>
          <Trans>Review</Trans>
        </Button>
      }
    >
      <Plural value={count} one="# sign-off is waiting for your confirmation." other="# sign-offs are waiting for your confirmation." />
    </Alert>
  )
}
