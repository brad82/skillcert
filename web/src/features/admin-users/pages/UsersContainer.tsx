import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useCurrentUser } from '@features/current-user'
import { problemType } from '@shared/api/client'
import { useSetUserActive, useSetUserClassification } from '../api/adminUsersApi.gen'
import { usersQueryOptions } from '../model/usersQuery'
import { UsersProvider } from '../UsersProvider'
import { UsersPage } from './UsersPage'

/** Owns the users query and the active/classification mutations; refreshes the list after each change. */
export function UsersContainer() {
  const { t } = useLingui()
  const { id: currentUserId } = useCurrentUser()
  const queryClient = useQueryClient()
  const { data } = useSuspenseQuery(usersQueryOptions())
  const refresh = () => queryClient.invalidateQueries({ queryKey: usersQueryOptions().queryKey })
  const setActive = useSetUserActive({ mutation: { onSettled: refresh } })
  const setClassification = useSetUserClassification({ mutation: { onSettled: refresh } })
  const failed = setActive.error ?? setClassification.error

  function errorText(): string | null {
    if (!failed) return null
    if (problemType(failed) === 'user.self-deactivation') return t`You can't deactivate your own account.`
    return t`The change couldn't be saved. Please try again.`
  }

  return (
    <UsersProvider
      users={data.users}
      classifications={data.classifications}
      currentUserId={currentUserId}
      busy={setActive.isPending || setClassification.isPending}
      error={errorText()}
      onSetActive={(userId, isActive) => {
        setClassification.reset()
        setActive.mutate({ userId, data: { isActive } })
      }}
      onSetClassification={(userId, code, holds) => {
        setActive.reset()
        setClassification.mutate({ userId, code, data: { holds } })
      }}
    >
      <UsersPage />
    </UsersProvider>
  )
}
