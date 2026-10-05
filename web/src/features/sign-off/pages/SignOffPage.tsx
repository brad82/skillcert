import Box from '@mui/material/Box'
import { Assess } from '../components/Assess'
import { EmptySignOff } from '../components/EmptySignOff'
import { FindReviewer } from '../components/FindReviewer'
import { Handoff } from '../components/Handoff'
import { ReviewerBar } from '../components/ReviewerBar'
import { SignOffResult } from '../components/SignOffResult'
import { SignStep } from '../components/SignStep'
import { useSignOffChrome } from '../SignOffProvider'

const screens = { empty: EmptySignOff, handoff: Handoff, findReviewer: FindReviewer, assess: Assess, sign: SignStep, result: SignOffResult }

const signOffPageStyles = () => ({
  page: { minHeight: '100dvh', bgcolor: 'background.default' },
  body: { px: 4, py: 6, maxWidth: 720, mx: 'auto', pb: 'calc(24px + env(safe-area-inset-bottom, 0px))' },
})

/** Wireframe 5a. Full screen (no shell chrome): reviewer mode adds its black bar. Layout only. */
export function SignOffPage() {
  const { screen, reviewerMode } = useSignOffChrome()
  const Screen = screens[screen]
  const styles = signOffPageStyles()

  return (
    <Box sx={styles.page}>
      {reviewerMode && <ReviewerBar />}
      <Box component="main" sx={styles.body}>
        <Screen />
      </Box>
    </Box>
  )
}
