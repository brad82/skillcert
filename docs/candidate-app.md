# Candidate app: chosen screens

Source wireframes: Claude Design project "Candidate App Wireframes"
(https://claude.ai/design/p/526d8a94-8aa1-4a89-aa91-10e6012a4412). The IDs below (1b, 3c…) refer to
options in that file.
- **Structure** (what's on each screen, order, behaviour) comes from the wireframes.
- **Look** (colour, type, spacing, status chips) comes from the design system:
  https://claude.ai/artifact/7AzLQcJz1v5Nk6J3zG7xor.

Mobile-first and MUI-shaped: app bar, bottom navigation, chips, bottom sheets.

## Navigation

Bottom nav, always 5 tabs: **Home · Skills · Basket · Events · Records**.
- Events (Phase 6) and Records (Phase 3) show a "coming soon" screen until their phase.
- The Basket tab carries a count badge.

## Status chips

| State | Wireframe mark | Design-system tokens |
| --- | --- | --- |
| Current | ✓ | `status-current-*` |
| Expiring soon (≤ 30 days; presentation only) | dashed, date | Current colours plus "Expires …" text |
| Expired | ! | `status-expired-*` |
| Not competent | ✕ | `status-not-competent-*` |
| Pending confirmation | ◔ | `status-pending-*` |
| Not certified | ○ | `status-not-certified-*` |

## 01 Home: 1b, status tiles and next event

- **Compliance card for each required list:** title, "Compliant" / "Not compliant · N to go", status mark.
- **Four tiles:** Expired · Expiring ≤ 30d · Pending · Not certified (counts). Tapping a tile opens the
  skills list filtered to that state.
- **"Your next event":** Phase 6. Until then, a quiet empty state.

## 02 Skills list: 2a, accordion tree

- One page per list, keeping the record's hierarchy.
- **Top of page:** search, then filter chips **All · Needs action · Current**.
- **Heading rows:** expand and collapse, and show "current / total" for their subtree. Groups where
  everything is current start collapsed.
- **Competency rows:** a checkbox (add to or remove from the basket), the code, the short title and a
  status chip. Tapping the row opens the detail screen.
- **Basket feedback:** a snackbar "N in basket · REVIEW" and the Basket tab badge.
- **Pending items** can't be added (the checkbox is disabled).

## 03 Competency detail: 3b tabs (Overview 3c, Resources 3d, History 3b)

- **App bar:** ‹ code and short title.
- **Status chip pinned above the tabs**, e.g. "Expiring 9 Nov 2026", "Pending confirmation".
- **Overview:**
  - full title
  - key facts as label/value pairs: Last signed, By (name · method), Expires (date and days left),
    Recertify ("Every 1 year" for 365 days, display only), Revision (number · published date)
  - "Can be signed by: <lowest method> or higher"
  - description
  - "Part of: List › Section › Sub-section"
  - **Pending:** a dashed box reading "Signed <date> by <name> (Supervisor). Becomes Current once <name>
    confirms."
- **Resources:** grouped by type: Video (thumbnail card), Web page, Document. All open externally (↗).
  Empty state: "No resources for this skill."
- **History:**
  - a timeline of reviews, newest first: outcome, method, date, reviewer, revision, confirmation state
  - expiry events are shown inline
  - tapping a review shows reviewer, signature, comment and the other skills signed in the same sitting
    (same signature, presentation only)
- **Bottom action:**
  - "Add to basket" when not current
  - "Add to basket for reassessment" when current
  - disabled "Waiting for confirmation" when pending
  - "Remove from basket" when it's already in the basket (not in the wireframe)

## 04 Basket: 4a, grouped by who can sign

- **Groups:** items split into groups by their **lowest permitted reviewer** (Self → Peer → Instructor →
  Supervisor), titled "<method> or higher". Higher methods are implied (POC decision, development plan §2.5).
- **Each group:** its items (code, title, ✕ remove) and "Sign off these N".
- **Merge suggestion:** "An Instructor could sign all N → merge" when one method covers everything.
- **App bar:** "Basket (N)" and CLEAR.

## 05 Sign-off: 5a, hand-off into Reviewer mode

1. **Hand-off:** "Pass your phone to the <method>", the candidate's name and skill count, then
   "I'm the reviewer →" or Cancel.
2. **Reviewer mode** has a black app bar with EXIT and no bottom nav. EXIT returns to the candidate and
   saves nothing.
   1. **Find your name:** search registered users who can sign this group. Peers are any other
      registered user; no typed names.
   2. **Assess <candidate>:** every item defaults to Competent and the reviewer flips the exceptions to
      Not competent. Optional comment.
   3. **Sign:** "<reviewer> (<method>) confirms N Competent, M Not competent for <candidate>, <date>",
      then the signature pad (clear), then "Submit & hand back".
3. **Result:** "N reviews recorded", with outcome chips. For a Supervisor sign-off: "Waiting for
   <name> to confirm" and the Pending chip. Then "Back to basket".

The whole flow is full-screen (no app bar or bottom nav), so the reviewer never sees the candidate's tabs.
Self and Peer reviewers skip the signature pad (development plan §2.7).

## Approval queue (no wireframe)

`/approvals`, reached from a Home banner ("N sign-offs are waiting for your confirmation"). One card per
sitting: candidate, date, skills with their verdicts, comment and the signature, then **Reject** (reason
required, final) or **Confirm all**.

## 06 Events: 6a (Phase 6)

Tabs Open · My events · Past, with a card list and "Scan QR". The detail screen shows your status per skill,
and its button cycles Register / Withdraw / Check in.

## 07 Records: 7a (Phase 3)

A card for each required list: compliance, a progress bar and "Download current record (PDF)". Below it,
"Archived records" (saved automatically when you became compliant).

## Not taken from the wireframes

- **Typed peer names** ("Not listed — type name"): spec §10 requires registered users.
- **CSP#** on the hand-off and records: identity is the name only (development plan §2.3).
- **"Correlated items"** in history: only shown as reviews sharing a signature. No batch identity is
  stored (spec §9).
