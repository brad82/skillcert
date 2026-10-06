# Phase 6: brief for the groups, opportunities and attestation wireframes

This is what the Phase 6 screens need to show and do. It's written for wireframing in Claude Design: each
section is one screen-sized area, with the data available, the actions, and the refusals the UI must handle.

- **The API isn't built yet.** The screens come first; the contract is then written to match them, as in
  Phase 4. Field names here are indicative. Refusal codes are the planned problem `type`s.
- Product rules: spec §6 and §14–17. Defaults settled for the POC: development plan §2.4.
- Look and feel: the design system (https://claude.ai/artifact/7AzLQcJz1v5Nk6J3zG7xor). Existing patterns to
  reuse: the admin shell (wireframes https://claude.ai/artifact/Br53vPE17YVictB8qjjSY9) and the candidate app
  (`docs/candidate-app.md`, Claude Design project "Candidate App Wireframes", option 6a for Events).
- Every admin change is **audited** (spec §23). Opportunities are **never deleted**; groups and users are
  deactivated, never deleted.

**Three audiences, three devices:**

| Who | Where | Device |
| --- | --- | --- |
| Administrator | Admin area: Groups and Opportunities sections | Desktop first |
| Instructor (assigned to an opportunity) | Candidate app, own login | **Tablet first**, must work on a phone |
| Candidate (any user) | Candidate app, Events tab and QR landing | Phone first |

**Data sizes to design for:** about 500 users, 3–10 groups, 10–50 opportunities a season, up to 30
participants and 30 competencies on one opportunity (a 30 × 30 attestation grid).

---

## Part A: Admin (desktop)

### 1. Groups list

Groups decide which competency lists a user must complete (spec §6). Today they're seed-only: New Patroller,
Returning Patroller, Senior Patroller.

| Field | Notes |
| --- | --- |
| name, description | Name is unique, ignoring case |
| isActive | An inactive group keeps its members and lists but **imposes no requirements** |
| memberCount | |
| lists | Titles of assigned competency lists |

**Actions:** create (name, optional description), open a group.

**Refusals:** `group.duplicate-name` (409).

### 2. Group detail

Three areas on one page:

1. **Details:** rename, edit description, deactivate / reactivate.
   - Deactivating removes the group's lists from every member's requirements straight away. The UI should say
     how many users that affects.
2. **Assigned lists:** add from the existing lists, remove.
   - A user's required lists are the union over all their active groups, so a list stays required if another
     group also assigns it.
3. **Members:** a table (name, email, other groups) with search; add users (search by name or email, multi-select),
   remove.
   - Deactivated users can stay members but are shown as inactive.

Changes are live: candidates see new requirements on their next load. Nothing touches review history.

**Link-ups:** the Users screen already lists each user's groups (Phase 4); those names should link here. A
"History" link opens the audit log filtered to this group.

**Audit actions:** `group.create`, `group.rename`, `group.deactivate`, `group.reactivate`, `group.add-members`,
`group.remove-member`, `group.assign-list`, `group.unassign-list`.

### 3. Opportunities list

| Column | Notes |
| --- | --- |
| title | |
| startsAt – endsAt | Local time |
| location | Free text |
| status | Draft · Open · InProgress · Completed · Cancelled |
| seats | "18 / 24" (Registered + Attended / capacity), or "18" when there's no capacity |
| instructors | Names |
| audience | "Everyone" or group names |

**Filters:** status (default: everything except Completed and Cancelled), date range, search on title or
location.

**Actions:** create, open, duplicate (copies the definition into a new Draft; no registrations).

### 4. Opportunity editor

The admin defines and configures the opportunity. Instructors can't create or edit one (spec §14).

**Definition**

| Field | Rules |
| --- | --- |
| title | Required, max 200 |
| description | Optional, max 4000 |
| location | Free text, optional, max 200 |
| startsAt, endsAt | Required, endsAt after startsAt. Not used to change status (spec §17) |
| capacity | Optional positive whole number. Empty = no limit. Hard limit, no waitlist |

**Competencies** (at least one to open)
- Add from the competency library (same picker as the list tree editor), or **copy from a list**: adds every
  distinct competency in that list once. It's a one-off copy; later list edits don't change the opportunity.
- A competency appears once per opportunity. Reorder for display; remove.
- **Flag competencies an Instructor can't sign** (lowest reviewer is Supervisor). They can stay on the
  opportunity but their attestation column will be locked. Deactivated competencies are flagged the same way.

**Instructors** (at least one to open)
- Choose from users who **currently hold the Instructor classification**. Several are allowed.
- If someone loses the classification later they stay listed but can't attest; show a warning.

**Audience**
- **Everyone** or **members of selected groups** (one or more active groups).
- Checked at registration only. Changing the audience never removes existing registrations.

**Editing by status**

| Status | Admin can edit |
| --- | --- |
| Draft, Open, InProgress | Everything. Capacity can't drop below seats already taken |
| Completed, Cancelled | Nothing: read-only history |

**Refusals:** `opportunity.capacity-below-seats` (409), `opportunity.duplicate-competency` (409), plus field
validation (400).

### 5. Opportunity detail (admin)

A header with title, dates, location, status chip, seats, and the **lifecycle buttons** allowed now:

| From | Button | To | Who | Notes |
| --- | --- | --- | --- | --- |
| Draft | Open for registration | Open | Admin | Needs ≥ 1 competency and ≥ 1 instructor |
| Draft | Cancel | Cancelled | Admin | Unused drafts only (plan §2.4) |
| Open | Start event | InProgress | Admin or assigned instructor | |
| Open | Cancel | Cancelled | Admin | Ask for confirmation; registrations stay on record |
| InProgress | Mark completed | Completed | Admin or assigned instructor | |

Nothing else, and nothing changes on its own when the clock passes startsAt or endsAt.

Below the header, tabs:
- **Overview:** definition, competencies, instructors, audience, and an "Edit" action.
- **Participants:** the same participant table instructors see (§7), plus **admin override** of attendance at
  any status, including after Completed. Overrides are audited.
- **Attestations:** each attestation run, showing when, by whom, and how many Competent / Not competent /
  not assessed. Read-only.
- **History:** audit entries for this opportunity.

**Refusals:** `opportunity.invalid-transition` (409), `opportunity.not-ready` (409, with what's missing).

**Audit actions:** `opportunity.create`, `opportunity.edit`, `opportunity.set-competencies`,
`opportunity.set-instructors`, `opportunity.set-audience`, `opportunity.open`, `opportunity.start`,
`opportunity.complete`, `opportunity.cancel`, `registration.override-attendance`.

---

## Part B: Instructor (own device, tablet first)

Instructors are ordinary users, so this lives in the candidate app, not the admin area. Spec §18: an
instructor sees **only their assigned opportunities** and no organisation-wide records.

### 6. Teaching list

How an instructor gets here is a design question (see the end). It lists their assigned opportunities:
- **Upcoming and live:** Open and InProgress, soonest first
- **Past:** Completed (read-only) and Cancelled

Each card shows title, date and time, location, status and "12 registered · 9 attended".

### 7. Participants and attendance

The working screen on the day.

**Header:** title, time, location, status; actions **Show QR**, **Start event** (when Open), **Attest** (when
InProgress), **Mark completed** (when InProgress).

**Participant table**

| Field | Notes |
| --- | --- |
| name | |
| state | Registered · Attended · Withdrawn |
| registeredAt, checkedInAt | |
| attested | Whether this run has reviews for them yet (e.g. "Attested 14:05") |

- **Toggle Registered ↔ Attended** per person, plus "Mark all present". Allowed while Open or InProgress
  (plan §2.4).
- Withdrawn people are shown separately and can't be toggled by the instructor.
- Counts: registered, attended, seats left.
- After Completed the screen is read-only for instructors.

**Refusals:** `opportunity.not-assigned` (403), `opportunity.read-only` (409, Completed or Cancelled).

### 8. Show QR

Full screen, for candidates to scan from across a room:
- a large QR code, the title, and "Scan to check in"
- a short link under it, for anyone whose camera won't scan
- a close button

The QR holds a signed token naming the opportunity; it isn't stored. It works while the opportunity is Open
or InProgress, and still goes through the audience and capacity rules (spec §15).

### 9. Attestation grid

The instructor assesses everyone at once from their own logged-in device (spec §16). Thirty candidates by
thirty competencies is 900 cells; design for that, not for 5 × 5.

**Rows:** attended candidates. **Columns:** the opportunity's competencies (code and short title).

**Each cell has three states:**
- **Competent:** the default for every cell
- **Not assessed:** creates no review
- **Not competent:** must be chosen deliberately, never the result of un-ticking

**Bulk actions:** whole row or column → Not assessed; whole row or column → Competent. **There is no bulk Not
competent.**

**Helpful context, optional:** a small mark for each candidate's current status on that competency, e.g.
already Current until 2027.

**Locked columns:** competencies an Instructor can't sign, or that are deactivated, show greyed with the reason
and create nothing.

**Then:**
1. **Summary:** "You're recording 812 Competent and 3 Not competent for 30 candidates; 85 not assessed."
   Optional comment.
2. **Sign** once on the signature pad (same component as candidate sign-off).
3. **Submit.** One transaction; every review shares the time, the instructor, their Instructor classification,
   the signature and the opportunity. The reviews are immediately effective.
4. **Result:** counts, then back to participants.

Running attestation again is allowed (a later review simply supersedes, spec §12). The grid should warn
"You already attested this group at 14:05" and show which cells already have a review from this opportunity.

**Refusals:** `opportunity.not-assigned` (403), `attest.not-instructor` (403: no Instructor classification
now), `opportunity.wrong-status` (409: only InProgress), `attest.candidate-not-attended` (409),
`attest.competency-not-signable` (409), `attest.empty` (400: nothing to record).

### 10. Mark completed

A confirmation: "Mark <title> completed? Attendance and attestation will be read-only." If some attended
candidates have no reviews from this opportunity, say how many. Completing is allowed anyway (spec §17:
completed doesn't mean everyone was assessed).

---

## Part C: Candidate (phone first)

Wireframe option 6a already exists: tabs **Open · My events · Past**, a card list, and **Scan QR**.

### 11. Events tab

- **Open:** Open opportunities the user is eligible for (audience), soonest first. Card: title, date and time,
  location, "6 seats left" or "Full", and "Registered" when they are.
- **My events:** their Registered and Attended opportunities that aren't Completed or Cancelled.
- **Past:** Completed or Cancelled ones they registered for.
- **Scan QR:** opens the camera; the phone's own camera app also works, through the short link (§13).

### 12. Event detail

- title, dates, location, description, instructors, seats left
- the competencies covered, each with **the candidate's current status chip** (same chips as Skills), so
  they can see what the event would renew
- their registration state

**Bottom button**

| Situation | Button |
| --- | --- |
| Not registered, Open, seats left | Register |
| Not registered, Open, full | Disabled "Full" |
| Registered or Withdrawn, Open | Withdraw / Register again (plan §2.4) |
| InProgress | "Check in": scan the QR, or ask the instructor |
| Attended | No button; "You're checked in" |
| Cancelled | No button; "Cancelled" banner |

After attestation, the reviews appear in the normal Skills screens with "at <opportunity title>" in history.

### 13. QR landing

The QR and short link open one screen. If signed out: sign in, then return here.

1. **Confirm:** title, time, location, and one button.
   - **Register** while Open
   - **Check in** while InProgress: registers if needed and marks Attended in one step, so walk-ins land on
     the instructor's list
2. **Result:** "You're registered" / "You're checked in", then "View event".

**Refusals the screen must explain in plain words:**
- `registration.full` (409)
- `registration.not-eligible` (403): not in the audience groups
- `registration.closed` (409): Draft, Completed or Cancelled
- `registration.invalid-token` (400): the QR is damaged or for a different system

### 14. Home: "Your next event"

The card already reserved on Home (option 1b): the user's next Registered or Attended opportunity that isn't
Completed or Cancelled, with title, date and time, location, and a link to the detail. If there's none: "No
upcoming events · Browse events".

---

## Questions for the wireframes to explore

1. **How instructors reach their screens.** Options: a fourth tab inside Events ("Teaching"); a Home banner
   like the approval queue's; or a separate instructor mode. The bottom nav stays at five tabs.
2. **The 30 × 30 grid on a tablet.** Sticky names and headers, fast bulk actions, and a clear visual difference
   between Not assessed and Not competent. Also a phone fallback, e.g. one candidate or one competency per
   screen.
3. **The admin opportunity editor:** one long page, or steps (Details → Competencies → Instructors →
   Audience)?
4. **Group membership at scale:** adding 40 users to a group without 40 separate searches.

## Out of scope (spec §24)

Waitlists, payments, recurring events, calendar integration, notifications, instructor-created
opportunities, and recommending opportunities to candidates.
