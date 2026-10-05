import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { type BasketItem, useBasket } from '@features/basket'
import { useCurrentUser } from '@features/current-user'
import { invalidateMyRecord } from '@features/my-record'
import { problemType } from '@shared/api/client'
import { useSignOff } from '../api/signOffApi.gen'
import { signOffReviewersQueryOptions } from '../model/signOffQueries'
import { SignOffProvider } from '../SignOffProvider'
import { SignOffPage } from './SignOffPage'

type Props = {
  competencyIds: string[]
  onCancel: () => void
  onDone: () => void
}

/**
 * Owns the reviewer query and the sign-off mutation. The items are taken from the basket once, when the flow
 * opens, so removing them after a successful sign-off doesn't empty the result screen.
 */
export function SignOffContainer({ competencyIds, onCancel, onDone }: Props) {
  const basket = useBasket()
  const [items] = useState(() => basket.items.filter((i) => competencyIds.includes(i.competencyId)))
  if (items.length === 0) {
    return (
      <SignOffProvider items={[]} candidateName="" reviewers={[]} submitting={false} error={null} result={null} onSubmit={() => {}} onCancel={onCancel} onDone={onDone}>
        <SignOffPage />
      </SignOffProvider>
    )
  }
  return <SignOffWithItems items={items} onCancel={onCancel} onDone={onDone} />
}

function SignOffWithItems({ items, onCancel, onDone }: { items: BasketItem[]; onCancel: () => void; onDone: () => void }) {
  const { t } = useLingui()
  const basket = useBasket()
  const { displayName } = useCurrentUser()
  const queryClient = useQueryClient()
  const { data } = useSuspenseQuery(signOffReviewersQueryOptions(items.map((i) => i.competencyId)))
  const signOff = useSignOff()

  function errorText(): string | null {
    if (!signOff.error) return null
    switch (problemType(signOff.error)) {
      case 'signoff.pending':
        return t`One of these skills is already waiting for confirmation. Remove it and try again.`
      case 'review.method-not-permitted':
        return t`This reviewer can't sign off all of these skills.`
      case 'signoff.signature-required':
        return t`Please sign before submitting.`
      case 'signature.invalid':
        return t`The signature couldn't be read. Clear it and sign again.`
      default:
        return t`The sign-off couldn't be recorded. Please try again.`
    }
  }

  return (
    <SignOffProvider
      items={items}
      candidateName={displayName}
      reviewers={data.reviewers}
      submitting={signOff.isPending}
      error={errorText()}
      result={signOff.data ?? null}
      onSubmit={(request) =>
        signOff.mutate(
          { data: request },
          {
            onSuccess: async () => {
              basket.removeMany(request.items.map((i) => i.competencyId))
              await invalidateMyRecord(queryClient)
            },
          },
        )
      }
      onCancel={onCancel}
      onDone={onDone}
    >
      <SignOffPage />
    </SignOffProvider>
  )
}
