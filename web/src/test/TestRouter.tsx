import { createMemoryHistory, createRootRoute, createRouter, RouterProvider } from '@tanstack/react-router'
import type { ReactNode } from 'react'

/**
 * A bare in-memory router around a page, for page tests whose components render router links.
 * Nothing navigates: the root route just renders the children. Content appears asynchronously, so use findBy*.
 */
export function TestRouter({ children }: { children: ReactNode }) {
  const router = createRouter({
    routeTree: createRootRoute({ component: () => children }),
    history: createMemoryHistory(),
  })
  return <RouterProvider router={router} />
}
