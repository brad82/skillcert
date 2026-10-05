import { useState } from 'react'
import type { AdminRevisionDto, ResourceType, RevisionContentRequest } from '@shared/api/model'
import { type SigningAuthority, signingAuthorities, signingAuthorityOf } from '@shared/lib/reviewers'

export type EditorTab = 'details' | 'resources' | 'certification'
export type ChangedField = 'title' | 'shortTitle' | 'description' | 'links' | 'recertificationDays' | 'signingAuthority'
export type Link = { title: string; url: string; type: ResourceType }
export type FieldProblem = 'required' | 'tooLong' | 'invalid' | null

export const tabOf: Record<ChangedField, EditorTab> = {
  title: 'details',
  shortTitle: 'details',
  description: 'details',
  links: 'resources',
  recertificationDays: 'certification',
  signingAuthority: 'certification',
}

export type CompetencyDraft = {
  title: string
  shortTitle: string
  description: string
  links: Link[]
  recertificationDays: string
  authority: SigningAuthority
  setTitle: (value: string) => void
  setShortTitle: (value: string) => void
  setDescription: (value: string) => void
  setLink: (index: number, link: Partial<Link>) => void
  addLink: () => void
  removeLink: (index: number) => void
  setRecertificationDays: (value: string) => void
  setAuthority: (value: SigningAuthority) => void
  discard: () => void
  /** Which fields differ from the current revision, in tab order. */
  changes: ChangedField[]
  problems: { title: FieldProblem; shortTitle: FieldProblem; description: FieldProblem; recertificationDays: FieldProblem; links: FieldProblem[] }
  valid: boolean
  toContent: () => RevisionContentRequest
}

const maxLinks = 20
const httpUrl = /^https?:\/\/\S+$/i

const fromRevision = (revision: AdminRevisionDto) => ({
  title: revision.title,
  shortTitle: revision.shortTitle ?? '',
  description: revision.description ?? '',
  links: revision.resources.map((r) => ({ title: r.title, url: r.url, type: r.type })),
  recertificationDays: revision.recertificationDays?.toString() ?? '',
  authority: signingAuthorityOf(revision.lowestReviewer),
})

/**
 * The competency editor's working copy of the current revision. Every tab edits it; "Publish changes" then
 * decides whether it becomes an editorial edit or a new revision. Mirrors the API's limits (title and short
 * title ≤ 300, description ≤ 4000, ≤ 20 links with a full http(s) address, days 1–36500 or empty).
 * Knows nothing about the API or the review flow.
 */
export function useCompetencyDraft(revision: AdminRevisionDto): CompetencyDraft {
  const original = fromRevision(revision)
  const [draft, setDraft] = useState(original)
  const set = (patch: Partial<typeof original>) => setDraft((current) => ({ ...current, ...patch }))

  const days = draft.recertificationDays.trim()
  const daysValue = days === '' ? null : Number(days)
  const tooLong = (value: string, max: number) => (value.length > max ? ('tooLong' as const) : null)
  const problems = {
    title: draft.title.trim() === '' ? ('required' as const) : tooLong(draft.title, 300),
    shortTitle: tooLong(draft.shortTitle, 300),
    description: tooLong(draft.description, 4000),
    recertificationDays: daysValue !== null && !(Number.isInteger(daysValue) && daysValue >= 1 && daysValue <= 36500) ? ('invalid' as const) : null,
    links: draft.links.map((link) => (link.title.trim() === '' ? ('required' as const) : httpUrl.test(link.url.trim()) ? null : ('invalid' as const))),
  }

  const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b)
  const trimmedLinks = (links: Link[]) => links.map((l) => ({ ...l, title: l.title.trim(), url: l.url.trim() }))
  const changes = (
    [
      ['title', draft.title.trim() !== original.title],
      ['shortTitle', draft.shortTitle.trim() !== original.shortTitle],
      ['description', draft.description.trim() !== original.description],
      ['links', !same(trimmedLinks(draft.links), original.links)],
      ['recertificationDays', days !== original.recertificationDays],
      ['signingAuthority', draft.authority !== original.authority],
    ] as [ChangedField, boolean][]
  )
    .filter(([, changed]) => changed)
    .map(([field]) => field)

  return {
    ...draft,
    setTitle: (title) => set({ title }),
    setShortTitle: (shortTitle) => set({ shortTitle }),
    setDescription: (description) => set({ description }),
    setLink: (index, link) => set({ links: draft.links.map((l, i) => (i === index ? { ...l, ...link } : l)) }),
    addLink: () => draft.links.length < maxLinks && set({ links: [...draft.links, { title: '', url: '', type: 'WebPage' }] }),
    removeLink: (index) => set({ links: draft.links.filter((_, i) => i !== index) }),
    setRecertificationDays: (recertificationDays) => set({ recertificationDays }),
    setAuthority: (authority) => set({ authority }),
    discard: () => setDraft(original),
    changes,
    problems,
    valid: !problems.title && !problems.shortTitle && !problems.description && !problems.recertificationDays && problems.links.every((p) => p === null),
    toContent: () => ({
      title: draft.title.trim(),
      shortTitle: draft.shortTitle.trim() || null,
      description: draft.description.trim() || null,
      recertificationDays: daysValue,
      lowestReviewer: signingAuthorities.find((a) => a.key === draft.authority)!.level,
      resources: trimmedLinks(draft.links),
    }),
  }
}
