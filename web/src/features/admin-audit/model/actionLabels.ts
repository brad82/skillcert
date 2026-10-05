import { msg } from '@lingui/core/macro'
import type { MessageDescriptor } from '@lingui/core'

/** Plain words for each audited action; an unknown action shows its code. */
export const actionLabels: Record<string, MessageDescriptor> = {
  'user.deactivate': msg`Deactivated a user`,
  'user.reactivate': msg`Reactivated a user`,
  'user.assign-classification': msg`Assigned a classification`,
  'user.remove-classification': msg`Removed a classification`,
  'competency.create': msg`Created a competency`,
  'competency.import': msg`Imported from CSV`,
  'competency.edit-revision': msg`Edited the current revision`,
  'competency.publish-revision': msg`Published a new revision`,
  'competency.deactivate': msg`Deactivated a competency`,
  'competency.reactivate': msg`Reactivated a competency`,
  'list.create': msg`Created a list`,
  'list.rename': msg`Renamed a list`,
  'list.add-heading': msg`Added a heading`,
  'list.add-competencies': msg`Added competencies`,
  'list.rename-heading': msg`Renamed a heading`,
  'list.move-node': msg`Moved an item`,
  'list.remove-node': msg`Removed an item`,
}

export const entityTypeLabels: Record<string, MessageDescriptor> = {
  User: msg`User`,
  Competency: msg`Competency`,
  CompetencyList: msg`Competency list`,
}
