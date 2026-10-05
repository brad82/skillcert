import { useLingui } from '@lingui/react/macro'
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { isApiError, problemType } from '@shared/api/client'
import type { AdminCompetencyDto } from '@shared/api/model'
import { useEditCurrentRevision, usePublishRevision, useSetCompetencyActive } from '../api/adminCompetenciesApi.gen'
import { CompetencyEditorProvider } from '../CompetencyEditorProvider'
import { competenciesQueryOptions } from '../model/competenciesQuery'
import { competencyQueryOptions } from '../model/competencyQuery'
import { CompetencyEditorPage } from './CompetencyEditorPage'

type Props = {
  competencyId: string
  fromListId?: string
}

/**
 * Owns the competency query and the edit / publish / (de)activate mutations. After a save the returned
 * competency replaces the cached one and the provider remounts (keyed on the data's timestamp), so the draft
 * restarts from the new current revision. Lists are refreshed too: they show titles and active state.
 */
export function CompetencyEditorContainer({ competencyId, fromListId }: Props) {
  const { t } = useLingui()
  const queryClient = useQueryClient()
  const { data, dataUpdatedAt } = useSuspenseQuery(competencyQueryOptions(competencyId))
  const [failure, setFailure] = useState<unknown>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const edit = useEditCurrentRevision()
  const publish = usePublishRevision()
  const setActive = useSetCompetencyActive()

  async function save(change: () => Promise<AdminCompetencyDto>, done: (saved: AdminCompetencyDto) => string): Promise<boolean> {
    try {
      const saved = await change()
      queryClient.setQueryData(competencyQueryOptions(competencyId).queryKey, saved)
      setFailure(null)
      setNotice(done(saved))
      void queryClient.invalidateQueries({ queryKey: competenciesQueryOptions().queryKey })
      // Every list query (the index and each tree) shows competency titles and active state.
      void queryClient.invalidateQueries({ predicate: (query) => String(query.queryKey[0]).startsWith('/api/admin/lists') })
      return true
    } catch (error) {
      setFailure(error)
      return false
    }
  }

  function errorText(): string | null {
    if (!failure) return null
    if (problemType(failure) === 'competency.policy-change') return t`Recertification and signing authority can only change in a new revision.`
    if (isApiError(failure, 400)) return t`Some fields aren't valid. Check each tab and try again.`
    return t`The change couldn't be saved. Please try again.`
  }

  return (
    <CompetencyEditorProvider
      key={dataUpdatedAt}
      competency={data}
      fromListId={fromListId ?? null}
      busy={edit.isPending || publish.isPending || setActive.isPending}
      error={errorText()}
      notice={notice}
      onSaveEdit={(content) => save(() => edit.mutateAsync({ competencyId, data: content }), (c) => t`Saved to revision ${c.current.number}.`)}
      onPublish={(content, invalidatesPreviousReviews) =>
        save(
          () => publish.mutateAsync({ competencyId, data: { content, invalidatesPreviousReviews } }),
          (c) => t`Published revision ${c.current.number}.`,
        )
      }
      onSetActive={(isActive) =>
        save(
          () => setActive.mutateAsync({ competencyId, data: { isActive } }),
          (c) => (c.isActive ? t`Reactivated ${c.code}.` : t`Deactivated ${c.code}. It takes no new sign-offs.`),
        )
      }
    >
      <CompetencyEditorPage />
    </CompetencyEditorProvider>
  )
}
