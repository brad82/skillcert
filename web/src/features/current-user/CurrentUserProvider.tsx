import { createContext, type ReactNode, useContext } from 'react'
import type { Capability, CurrentUser } from './model/currentUserQuery'

const CurrentUserContext = createContext<CurrentUser | null>(null)

/**
 * Shares the signed-in user below the auth gate, so useCurrentUser() is never null.
 * Mounted once by the app shell. Does not fetch — the shell container owns the query.
 */
export function CurrentUserProvider({ user, children }: { user: CurrentUser; children: ReactNode }) {
  return <CurrentUserContext.Provider value={user}>{children}</CurrentUserContext.Provider>
}

export function useCurrentUser() {
  const user = useContext(CurrentUserContext)
  if (!user) throw new Error('useCurrentUser must be used within a CurrentUserProvider')
  return {
    ...user,
    hasCapability: (capability: Capability) => user.capabilities.includes(capability),
  }
}
