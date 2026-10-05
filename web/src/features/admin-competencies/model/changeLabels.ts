import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'
import type { ChangedField, EditorTab } from './useCompetencyDraft'

export const tabLabels: Record<EditorTab, MessageDescriptor> = {
  details: msg`Details`,
  resources: msg`Resources`,
  certification: msg`Certification`,
}

export const fieldLabels: Record<ChangedField, MessageDescriptor> = {
  title: msg`Title`,
  shortTitle: msg`Short title`,
  description: msg`Description`,
  links: msg`Links`,
  recertificationDays: msg`Recertification period`,
  signingAuthority: msg`Signing authority`,
}
