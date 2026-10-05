import { createFileRoute } from '@tanstack/react-router'
import { UsersContainer, usersQueryOptions } from '@features/admin-users'

export const Route = createFileRoute('/_authenticated/admin/users')({
  loader: ({ context }) => context.queryClient.ensureQueryData(usersQueryOptions()),
  component: UsersContainer,
})
