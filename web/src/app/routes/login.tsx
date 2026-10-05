import { createFileRoute } from '@tanstack/react-router'
import { LoginContainer } from '@features/auth'

type LoginSearch = {
  redirect?: string
}

export const Route = createFileRoute('/login')({
  // Only same-origin paths, so the redirect can't send someone off-site.
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    redirect:
      typeof search.redirect === 'string' && search.redirect.startsWith('/') && !search.redirect.startsWith('//')
        ? search.redirect
        : undefined,
  }),
  component: function LoginRoute() {
    const { redirect } = Route.useSearch()
    return <LoginContainer redirectTo={redirect} />
  },
})
