import Button from '@mui/material/Button'
import ListItemButton from '@mui/material/ListItemButton'
import MuiLink from '@mui/material/Link'
import { createLink } from '@tanstack/react-router'

/** MUI components wired to the router, with typed `to` / `params` / `search` (TanStack `createLink`). */
export const TextLink = createLink(MuiLink)
export const ButtonLink = createLink(Button)
export const ListItemLink = createLink(ListItemButton)
