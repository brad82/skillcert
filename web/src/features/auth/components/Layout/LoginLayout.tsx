import Box from '@mui/material/Box'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import type { ReactNode } from 'react'
import { LanguageSwitcher } from '@shared/components/LanguageSwitcher'
import { ThemeModeToggle } from '@shared/components/ThemeModeToggle'

const loginLayoutStyles = () => ({
  page: { minHeight: '100dvh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' },
  toolbar: { display: 'flex', justifyContent: 'flex-end', gap: 1, p: 1, color: 'text.secondary' },
  body: { flexGrow: 1, display: 'grid', placeItems: 'center', px: 2, pb: 6 },
  card: { width: '100%', maxWidth: 400 },
})

/** Centred card on a plain page, with language and theme controls. Chrome only. */
export function LoginLayout({ children }: { children: ReactNode }) {
  const styles = loginLayoutStyles()

  return (
    <Box sx={styles.page}>
      <Box sx={styles.toolbar}>
        <LanguageSwitcher />
        <ThemeModeToggle />
      </Box>
      <Box sx={styles.body}>
        <Card variant="outlined" sx={styles.card}>
          <CardContent>{children}</CardContent>
        </Card>
      </Box>
    </Box>
  )
}
