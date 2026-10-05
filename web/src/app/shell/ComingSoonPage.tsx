import { Trans } from '@lingui/react/macro'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Hammer } from 'lucide-react'
import type { ReactNode } from 'react'

/** Stands in for a bottom-nav tab whose feature arrives in a later phase (Events: 6, Records: 3). */
export function ComingSoonPage({ title }: { title: ReactNode }) {
  return (
    <Stack spacing={3} sx={{ alignItems: 'center', textAlign: 'center', pt: 12, color: 'text.secondary' }}>
      <Hammer size={40} aria-hidden />
      <Typography variant="h1" color="text.primary">
        {title}
      </Typography>
      <Typography>
        <Trans>This part of SkillCert is coming soon.</Trans>
      </Typography>
    </Stack>
  )
}
