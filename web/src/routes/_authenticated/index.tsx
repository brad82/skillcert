import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useSuspenseQuery } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { currentUserQueryOptions } from '../../auth'

export const Route = createFileRoute('/_authenticated/')({
  component: HomePage,
})

function HomePage() {
  const { data: me } = useSuspenseQuery(currentUserQueryOptions())

  return (
    <Card variant="outlined" sx={{ maxWidth: 480, mx: 'auto' }}>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          Signed in as
        </Typography>
        <Typography variant="h5" component="h1">
          {me.displayName}
        </Typography>
        <Typography color="text.secondary" gutterBottom>
          {me.email}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: 'wrap' }} useFlexGap>
          <Chip label="Candidate" />
          {me.capabilities.map((capability) => (
            <Chip key={capability} label={capability} color="primary" />
          ))}
        </Stack>
      </CardContent>
    </Card>
  )
}
