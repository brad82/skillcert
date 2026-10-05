import { useState } from 'react'
import type { AdminUserDto } from '@shared/api/model'

export type UserShow = 'all' | 'active' | 'deactivated'

export type UserFilters = {
  search: string
  setSearch: (search: string) => void
  show: UserShow
  setShow: (show: UserShow) => void
  apply: (users: AdminUserDto[]) => AdminUserDto[]
}

/**
 * The Users screen's search (name or email) and status filter. Filters the loaded list in the browser;
 * knows nothing about the API.
 */
export function useUserFilters(): UserFilters {
  const [search, setSearch] = useState('')
  const [show, setShow] = useState<UserShow>('all')
  const needle = search.trim().toLocaleLowerCase()
  return {
    search,
    setSearch,
    show,
    setShow,
    apply: (users) =>
      users.filter(
        (user) =>
          (show === 'all' || user.isActive === (show === 'active')) &&
          (needle === '' || user.displayName.toLocaleLowerCase().includes(needle) || user.email.toLocaleLowerCase().includes(needle)),
      ),
  }
}
