import { createContext, type ReactNode, useContext, useState } from 'react'
import type { AdminClassificationDto, AdminUserDto } from '@shared/api/model'
import { type PendingConfirm, usePendingConfirm } from './model/usePendingConfirm'
import { type UserFilters, useUserFilters } from './model/useUserFilters'

type Props = {
  users: AdminUserDto[]
  classifications: AdminClassificationDto[]
  /** The signed-in admin, who can't deactivate themselves. */
  currentUserId: string
  busy: boolean
  error: string | null
  onSetActive: (userId: string, isActive: boolean) => void
  onSetClassification: (userId: string, code: string, holds: boolean) => void
  children: ReactNode
}

type UsersContextValue = Omit<Props, 'children'> & {
  filters: UserFilters
  confirm: PendingConfirm
  selectedId: string | null
  select: (userId: string) => void
}

const UsersContext = createContext<UsersContextValue | null>(null)

/**
 * Composes the Users screen's filters, selection and confirmation state with the changes passed in.
 * Deactivating and removing a classification ask first; reactivating and assigning don't.
 */
export function UsersProvider({ children, ...props }: Props) {
  const filters = useUserFilters()
  const confirm = usePendingConfirm()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  return (
    <UsersContext.Provider value={{ ...props, filters, confirm, selectedId, select: setSelectedId }}>{children}</UsersContext.Provider>
  )
}

function useUsersContext() {
  const context = useContext(UsersContext)
  if (!context) throw new Error('Users hooks must be used within a UsersProvider')
  return context
}

export function useUsersHeader() {
  const { error } = useUsersContext()
  return { error }
}

export function useUserFilterFields() {
  return useUsersContext().filters
}

export function useUserRows() {
  const { users, filters, selectedId, select, currentUserId, busy, confirm, onSetActive } = useUsersContext()
  return {
    rows: filters.apply(users),
    selectedId,
    select,
    currentUserId,
    busy,
    deactivate: (userId: string) => confirm.ask({ kind: 'deactivate', userId }),
    reactivate: (userId: string) => onSetActive(userId, true),
  }
}

export function useSelectedUser() {
  const { users, classifications, selectedId, currentUserId, busy, confirm, onSetActive, onSetClassification } = useUsersContext()
  const user = users.find((u) => u.id === selectedId) ?? null
  return {
    user,
    classifications: [...classifications].sort((a, b) => a.rank - b.rank),
    isSelf: user?.id === currentUserId,
    busy,
    toggleClassification: (code: string, holds: boolean) => {
      if (!user) return
      if (holds) onSetClassification(user.id, code, true)
      else confirm.ask({ kind: 'removeClassification', userId: user.id, code })
    },
    deactivate: () => user && confirm.ask({ kind: 'deactivate', userId: user.id }),
    reactivate: () => user && onSetActive(user.id, true),
  }
}

export function useUserConfirm() {
  const { users, confirm, busy, onSetActive, onSetClassification } = useUsersContext()
  const { pending } = confirm
  const user = pending ? (users.find((u) => u.id === pending.userId) ?? null) : null
  return {
    pending,
    user,
    busy,
    cancel: confirm.cancel,
    confirm: () => {
      if (!pending) return
      if (pending.kind === 'deactivate') onSetActive(pending.userId, false)
      else onSetClassification(pending.userId, pending.code, false)
      confirm.cancel()
    },
  }
}
