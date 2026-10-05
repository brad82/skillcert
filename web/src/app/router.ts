import { createRouter } from '@tanstack/react-router'
import { queryClient } from './queryClient'
import { routeTree } from './routeTree.gen'
import { AppErrorScreen } from './shell/AppErrorScreen'

export const router = createRouter({
  routeTree,
  context: { queryClient },
  defaultPreload: 'intent',
  // TanStack Query owns caching; the router only triggers loads.
  defaultPreloadStaleTime: 0,
  defaultErrorComponent: AppErrorScreen,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
  interface StaticDataRouteOption {
    /** Hide the shell's app bar and bottom nav (sign-off's reviewer mode draws its own). */
    fullScreen?: boolean
  }
}
