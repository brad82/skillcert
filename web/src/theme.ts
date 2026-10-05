import { createTheme } from '@mui/material/styles'

// Placeholder palette taken from the AFA paper record. Replaced by Claude Design tokens in Phase 0, task 7.
export const theme = createTheme({
  palette: {
    primary: { main: '#a6192e' },
    secondary: { main: '#2f4858' },
  },
  shape: { borderRadius: 8 },
})
