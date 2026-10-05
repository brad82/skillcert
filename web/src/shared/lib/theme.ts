import '@fontsource-variable/public-sans'
import '@fontsource/ibm-plex-mono/400.css'
import '@fontsource/ibm-plex-mono/500.css'
import type { PaletteMode } from '@mui/material'
import { createTheme } from '@mui/material/styles'
import type { CSSProperties } from 'react'
import tokens from './design-tokens.json'

/**
 * Builds the MUI theme for a mode from the SkillCert design system's tokens.json
 * (https://claude.ai/artifact/7AzLQcJz1v5Nk6J3zG7xor). To pick up a token change, replace
 * design-tokens.json with the system's current project/tokens.json — nothing here holds a value.
 *
 * Mapping: MUI spacing unit = 4px, so `p: 4` is space-4; status colours live on
 * `palette.status.<state>.{bg,fg}`; type styles map to MUI variants (page-title → h1,
 * section-title → h2, subsection-title → h3, body → body1, body-sm → body2, label → overline)
 * plus the custom `code` and `date` variants. Knows nothing about per-feature colour — see
 * defineModePalette.
 */

type TokenName = (typeof tokens.color.tokens)[number]['name']
type StyleName = (typeof tokens.type.groups)[number]['styles'][number]['name']

const colour = (name: TokenName, mode: PaletteMode): string => {
  const token = tokens.color.tokens.find((t) => t.name === name)
  if (!token) throw new Error(`design-tokens.json has no colour token "${name}"`)
  return token.value[mode]
}

const lengthToken = (family: 'spacing' | 'radius', name: string): string => {
  const token = tokens[family].tokens.find((t) => t.name === name)
  if (!token) throw new Error(`design-tokens.json has no ${family} token "${name}"`)
  return token.value
}

type TypeStyle = {
  fontFamily: string
  fontSize: string
  lineHeight: string
  fontWeight: number
  letterSpacing?: string
}

const typeStyle = (name: StyleName): TypeStyle => {
  for (const group of tokens.type.groups) {
    const style = group.styles.find((s) => s.name === name)
    if (style) {
      return {
        fontFamily: tokens.type.families[group.family as keyof typeof tokens.type.families],
        fontSize: style.fontSize,
        lineHeight: style.lineHeight,
        fontWeight: style.fontWeight,
        ...('letterSpacing' in style ? { letterSpacing: style.letterSpacing } : {}),
      }
    }
  }
  throw new Error(`design-tokens.json has no type style "${name}"`)
}

export type StatusColours = { bg: string; fg: string }
export type CompetencyStatusKey = 'current' | 'expired' | 'notCompetent' | 'pending' | 'notCertified'

declare module '@mui/material/styles' {
  interface Palette {
    status: Record<CompetencyStatusKey, StatusColours>
    brandTint: string
    lineStrong: string
    focus: string
  }
  interface PaletteOptions {
    status?: Record<CompetencyStatusKey, StatusColours>
    brandTint?: string
    lineStrong?: string
    focus?: string
  }
  interface Theme {
    radius: { sm: string; md: string; pill: string }
  }
  interface ThemeOptions {
    radius?: { sm: string; md: string; pill: string }
  }
  interface TypographyVariants {
    code: CSSProperties
    date: CSSProperties
  }
  interface TypographyVariantsOptions {
    code?: CSSProperties
    date?: CSSProperties
  }
}

declare module '@mui/material/Typography' {
  interface TypographyPropsVariantOverrides {
    code: true
    date: true
  }
}

export function makeTheme(mode: PaletteMode) {
  const c = (name: TokenName) => colour(name, mode)
  const status = (key: string): StatusColours => ({
    bg: c(`status-${key}-bg` as TokenName),
    fg: c(`status-${key}-fg` as TokenName),
  })
  const radius = {
    sm: lengthToken('radius', 'radius-sm'),
    md: lengthToken('radius', 'radius-md'),
    pill: lengthToken('radius', 'radius-pill'),
  }

  return createTheme({
    palette: {
      mode,
      primary: { main: c('brand'), contrastText: c('brand-ink') },
      secondary: { main: c('focus'), contrastText: c('surface-raised') },
      success: { main: c('status-current-fg') },
      warning: { main: c('status-expired-fg') },
      error: { main: c('status-not-competent-bg'), contrastText: c('status-not-competent-fg') },
      info: { main: c('status-pending-fg') },
      background: { default: c('surface'), paper: c('surface-raised') },
      text: { primary: c('ink'), secondary: c('ink-muted') },
      divider: c('line'),
      status: {
        current: status('current'),
        expired: status('expired'),
        notCompetent: status('not-competent'),
        pending: status('pending'),
        notCertified: status('not-certified'),
      },
      brandTint: c('brand-tint'),
      lineStrong: c('line-strong'),
      focus: c('focus'),
    },
    // Spacing tokens are a 4px scale: theme.spacing(n) === space-n.
    spacing: 4,
    shape: { borderRadius: parseInt(radius.md, 10) },
    radius,
    typography: {
      fontFamily: tokens.type.families.sans,
      h1: typeStyle('page-title'),
      h2: typeStyle('section-title'),
      h3: typeStyle('subsection-title'),
      body1: typeStyle('body'),
      body2: typeStyle('body-sm'),
      overline: { ...typeStyle('label'), textTransform: 'uppercase' },
      button: { ...typeStyle('body'), fontWeight: 600, textTransform: 'none' },
      code: typeStyle('code'),
      date: { ...typeStyle('date'), fontVariantNumeric: 'tabular-nums' },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          // Focus ring from the brand book: 2px focus colour, offset 2px, on every interactive element.
          '*:focus-visible': { outline: `2px solid ${c('focus')}`, outlineOffset: 2 },
          '@media (prefers-reduced-motion: reduce)': {
            '*, *::before, *::after': { transitionDuration: '0.01ms !important', animationDuration: '0.01ms !important' },
          },
        },
      },
      MuiButtonBase: { defaultProps: { disableRipple: true } },
      MuiTypography: {
        defaultProps: {
          variantMapping: { code: 'span', date: 'span' },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          notchedOutline: { borderColor: c('line-strong') },
          root: { borderRadius: radius.sm },
        },
      },
      MuiChip: { styleOverrides: { root: { borderRadius: radius.pill } } },
      MuiCard: { defaultProps: { variant: 'outlined' } },
      // The brand book puts the record red on the app bar in both themes; MUI greys it in dark unless told.
      MuiAppBar: { defaultProps: { color: 'primary', elevation: 0, enableColorOnDark: true } },
      // The design system defines no shadows.
      MuiButton: { defaultProps: { disableElevation: true } },
    },
  })
}
