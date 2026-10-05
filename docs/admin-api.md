# Admin API: brief for the admin wireframes

This is what the admin screens can show and do. It's written for wireframing in Claude Design: each section is
one screen-sized area, with the data available, the actions, and the refusals the UI must handle.

- The full contract is the OpenAPI file `web/openapi/skillcert.json`, tags `AdminUsers`, `AdminCompetencies`,
  `AdminLists`, `AdminImport` and `AdminAudit`.
- Everything is under `/api/admin/…` and is **administrators only**:
  - signed-out callers get 401
  - everyone else gets 403
- Every change is **audited**: who, when, what, and before/after values (spec §23).
- Refusals are problem responses with a stable `type`, listed under each screen.
- Validation failures are 400, with errors keyed by field.

Screens the spec asks for (§19): Dashboard/Reporting (Phase 5), Users, Groups (Phase 6), Competencies,
Competency Lists, Reviewer Classification Assignments (part of Users here), Opportunities (Phase 6) and Audit.
Phase 4 covers **Users, Competencies, Lists, CSV import and Audit**.

Admin is desktop-first. The candidate app is phone-first and separate. A side navigation with the sections
above is a natural shell.

---

## 1. Users

`GET /api/admin/users?search=` returns:
- every user, ordered by name
- the assignable classifications, as `classifications[]` (code, name, rank)

The search matches name or email.

| Field | Notes |
| --- | --- |
| displayName, email | |
| isActive | Users are **deactivated, never deleted** (spec §5) |
| isAdministrator | Read-only here (no UI to grant admin in the POC) |
| classifications | Codes held now, e.g. `Instructor`, `Supervisor` |
| groups | Names of active groups they belong to (groups are managed in Phase 6) |
| createdAt | |

`GET /api/admin/users/{id}` returns one user, with the same fields.

**Actions**
- **Deactivate / reactivate:** `PUT /api/admin/users/{id}/active` with `{ isActive }`.
  - A deactivated user can't sign in or be chosen as a reviewer. Their history stays.
  - Refusal `user.self-deactivation` (409): an admin can't deactivate themselves.
- **Assign / remove a classification:** `PUT /api/admin/users/{id}/classifications/{code}` with `{ holds }`.
  - Removing one stops *new* reviews under it, but the user can still confirm or reject claims that already name
    them (spec §11). The UI could say so.

---

## 2. Competency library

`GET /api/admin/competencies?search=` lists every competency, active and inactive, in code order
(9.1a before 11.1). The search matches code or title.

| Field | Notes |
| --- | --- |
| code | e.g. `4.3.1`, `9.1a` |
| title, shortTitle | `shortTitle` is the paper wording shown under its heading; falls back to title |
| isActive | |
| revisionNumber | Current revision |
| recertificationDays | null = never expires. 365 reads as "every year" |
| lowestReviewer | `{ method: Self/Peer/Classified, classificationCode }` → "Instructor or higher" |
| listCount | How many lists use it |

### Competency detail and editor

`GET /api/admin/competencies/{id}` returns:
- `current`: the current revision, with title, shortTitle, description, recertificationDays, lowestReviewer,
  resources[] (title, url, type: WebPage/Video/Document), publishedAt and invalidatesPreviousReviews
- `revisions[]`: newest first, each with number, title, publishedAt, invalidatesPreviousReviews and
  recertificationDays
- `lists[]`: the lists that use it, with id and title

**Who can sign** is set as the *lowest* level: Self, Peer, Instructor or Supervisor. Every higher level is
implied (development plan §2.5).

**Three distinct actions.** The spec insists the UI keeps the first two apart (§3, §19):

1. **Edit current revision.** `PUT /api/admin/competencies/{id}/revision`
   - For editorial fixes to title, short title, description and resources. It never affects anyone's currency.
   - Refusal `competency.policy-change` (409): changing recertification or who can sign needs a new revision.
2. **Publish new revision.** `POST /api/admin/competencies/{id}/revisions` with `{ content, invalidatesPreviousReviews }`
   - The admin **must choose explicitly** between two outcomes:
     - *keep existing sign-offs*: everyone stays current
     - *require reassessment*: sign-offs on earlier revisions stop counting from now; affected candidates show
       "Not certified"
   - The wording and a confirmation step matter here. This is the most consequential action in admin.
   - Existing sign-offs keep the expiry interval of the revision they were signed against.
3. **Deactivate / reactivate.** `PUT /api/admin/competencies/{id}/active`
   - A deactivated competency takes no new sign-offs. Its history and list placements stay.

**Create.** `POST /api/admin/competencies` with `{ code, content }` creates revision 1.
- Refusal `competency.duplicate-code` (409): codes are unique after trimming and ignoring case, including
  inactive competencies.

Content fields and limits:
- title: required, max 300 characters
- shortTitle: max 300 characters
- description: max 4000 characters
- recertificationDays: 1–36500, or empty
- resources: max 20, each needing a title and a full http(s) link

---

## 3. Competency lists

`GET /api/admin/lists` returns each list's title, description, isActive, competencyCount and assigned groups.

`GET /api/admin/lists/{id}` returns the whole tree for the **tree editor**.

`nodes[]` comes in display order. Each node has id, parentNodeId, depth, index (position among its siblings),
kind (Heading or Competency) and:
- for a heading: headingCode (e.g. `4.1`) and headingTitle
- for a competency: `competency` (id, code, title, shortTitle, isActive)

The AFA record is 3 levels deep and mixes headings that hold competencies directly with headings that hold
sub-headings.

**Actions.** Each returns the whole updated list.

| Action | Endpoint |
| --- | --- |
| Create list | `POST /api/admin/lists` with `{ title, description }` |
| Rename list | `PUT /api/admin/lists/{id}` |
| Add heading | `POST /api/admin/lists/{id}/headings` with `{ parentNodeId?, code?, title, index? }` |
| Add competencies from the library | `POST /api/admin/lists/{id}/competencies` with `{ parentNodeId?, competencyIds[], index? }` (several at once) |
| Rename heading | `PUT /api/admin/lists/{id}/nodes/{nodeId}` with `{ code?, title }` |
| Move (drag and drop) | `PUT /api/admin/lists/{id}/nodes/{nodeId}/position` with `{ parentNodeId?, index }` |
| Remove (a heading takes its subtree) | `DELETE /api/admin/lists/{id}/nodes/{nodeId}` |

**Refusals (409)**
- `list.duplicate-competency`: a competency appears at most once per list. A picker could grey out those already
  in the list.
- `list.cycle`: a heading can't move inside itself.
- `list.invalid-parent`: only headings can hold items.

Changes are live: there are no list revisions. Compliance uses the current tree. Archived PDFs keep what they
showed. Removing items never touches review history.

---

## 4. CSV import (spec §26)

A two-step flow: **upload → preview → import**. The browser reads the file and sends its text, up to 1 MB.

1. **Preview.** `POST /api/admin/imports/competencies/preview` with `{ csv }` validates the whole file and
   changes nothing. It returns:
   - `canImport`
   - `fileErrors[]`, e.g. "Missing the Title column."
   - `rows[]`, each with line, code, title, lowestReviewer, recertificationDays, resourceCount, errors[] and
     warnings[]
2. **Import.** `POST /api/admin/imports/competencies` with the same `{ csv }` re-validates, then creates every
   competency as revision 1 in one go.
   - Any error anywhere → 422 with the same preview, and **nothing is created**. There is no partial import.
   - `import.conflict` (409): someone created one of the codes meanwhile; preview again.
   - Success returns `{ created, codes[] }`.

**File format** (development plan §2.10):

```
Code,Title,ShortTitle,Description,RecertificationDays,SelfReview,PeerReview,InstructorReview,SupervisorReview,Resources
13.1,Avalanche transceiver search,Transceiver search,,365,,,Y,,"Search basics|https://example.org/t|Video"
13.2,Probe line,,Organised probe line,730,,Y,,,
```

- Only Code and Title are required. Columns can be in any order.
- Method columns take Y or blank. The lowest marked level counts and higher levels are implied (a warning says
  so).
- Resources take the form `Title|URL|Type`, separated by `;`. Type is WebPage (the default), Video or Document.
- Typical errors to design for: missing code or title, a code repeated in the file or already in the library,
  bad day counts, unknown Y/N values, bad links.
- A warning that's always worth showing: an empty RecertificationDays means the skill never expires.

---

## 5. Audit log

`GET /api/admin/audit` returns newest first, 50 per page.

**Filters:** `entityType`, `entityId`, `actorUserId`, `from`, `to`. For the next page, pass `before=` the last
entry's `at`.

| Field | Notes |
| --- | --- |
| at | Server time |
| actorName | Who did it |
| action | e.g. `competency.publish-revision`, `list.move-node`, `user.deactivate` |
| entityType, entityLabel | `User` (name), `Competency` (code), `CompetencyList` (title) |
| before, after | JSON snapshots of the values that changed; show as a diff or key/value pairs |

`entityTypes[]` lists the types present, for the filter.

**Actions recorded so far:**
- `user.deactivate`, `user.reactivate`, `user.assign-classification`, `user.remove-classification`
- `competency.create`, `competency.import`, `competency.edit-revision`, `competency.publish-revision`,
  `competency.deactivate`, `competency.reactivate`
- `list.create`, `list.rename`, `list.add-heading`, `list.add-competencies`, `list.rename-heading`,
  `list.move-node`, `list.remove-node`

A competency or list detail screen can link to its own history (`?entityId=`).

---

## Notes from the admin screens (for API reconciliation)

The admin web was built from the wireframes against the API as it stands. These are the gaps it works around
or leaves out; each says what the screen does today.

1. **Shared count on list tree nodes.** The wireframes tag shared competencies "In N lists" in the list tree.
   `GET /api/admin/lists/{id}` returns each node's `competency` without a list count, so the tree shows no tag
   yet. Proposal: add `listCount` to `AdminListCompetencyDto`; the tree then shows the tag when it is above 1.
   (The competency editor and the review already show sharing, from `GET /api/admin/competencies/{id}` `lists[]`.)
2. **New competency in a list** is two calls from the browser: `POST /api/admin/competencies`, then
   `POST /api/admin/lists/{id}/competencies`. If the second fails, the competency exists but isn't in the list
   (the screen says so). An optional `{ listId, parentNodeId }` on create would make it one transaction.
3. **CSV import into a list** is also two calls: import, then the web app looks up the created codes in
   `GET /api/admin/competencies` and adds them to the chosen list. Returning the created ids from
   `POST /api/admin/imports/competencies` (or accepting an optional `{ listId, parentNodeId }`) would remove
   the lookup and make it one transaction.
4. **Searching** happens in the browser for users, lists and all competencies (the whole set is loaded once).
   The `search=` parameters are unused for now; worth switching to if the library grows to thousands.
5. **Audit date filters** send local midnight of the `from` day and of the day after `to`, as instants, so `to`
   covers its whole day. The API treats `to` as exclusive, which matches.
6. **Audit actor filter** lists administrators from `GET /api/admin/users`. A `GET /api/admin/audit/actors`
   (everyone who appears in the log, including since-demoted admins) would be more accurate.
7. **Recommendation rule** for "edit or new revision" is client-side (1 field → edit, 2+ → revision,
   certification → revision only). The API's only rule is `competency.policy-change`, which the screen
   already respects by not offering an edit.
