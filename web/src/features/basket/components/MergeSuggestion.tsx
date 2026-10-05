import { Trans, useLingui } from '@lingui/react/macro'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import { Merge } from 'lucide-react'
import { reviewLevelLabel } from '@shared/lib/reviewers'
import { useMergeSuggestion } from '../BasketPageProvider'

/** "Any Instructor could sign all N → merge", when one reviewer level covers every group. */
export function MergeSuggestion() {
  const { suggestion, merge } = useMergeSuggestion()
  const { i18n } = useLingui()
  if (!suggestion) return null

  const level = i18n._(reviewLevelLabel(suggestion.level))
  const count = suggestion.items.length

  return (
    <Alert
      severity="info"
      icon={<Merge size={20} aria-hidden />}
      action={
        <Button color="inherit" size="small" onClick={merge}>
          <Trans>Merge</Trans>
        </Button>
      }
    >
      <Trans>
        Any {level} could sign all {count} at once.
      </Trans>
    </Alert>
  )
}
