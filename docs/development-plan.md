# SkillCert POC: Development Plan

Source: `SkillCert_POC_Functional_Specification_Reconciled_2026-10-04_v1.2.pdf` (cited below as "spec §n").
Written: 4 October 2026.

## How to use this plan
- Built solo, part-time, mostly by Claude Code. Each `- [ ]` task fits in one Claude Code session.
- Work one phase at a time, in order. A phase is done only when every check under its **Done when** passes.
- Each phase starts by designing its screens in Claude Design. Implementation starts after the designs are reviewed.
- Estimates assume about 8–10 hours a week. **Total: about 17 weeks.**

| # | Phase | Spec § | Demo at end | Est. |
|---|---|---|---|---|
| 0 | Foundation + deploy pipeline | 5, 25 | App runs in Aspire and on the demo VPS; log in as seeded fake users | 1.5 wks |
| 1 | Domain core + currency engine | 2–4, 6–8, 12 | Unit tests prove every currency rule | 2 wks |
| 2 | Candidate sign-off + supervisor confirmation | 9–11, 13, 18 | Candidate signs off; supervisor confirms; status updates | 2–3 wks |
| 3 | Training record PDF + nightly archival | 21–22 | Download PDF that looks like the AFA record; nightly job archives it | 2 wks |
| 4 | Admin configuration + audit + CSV import | 3, 4, 19, 23, 26 | Admin edits/publishes revisions, edits list tree, imports CSV | 3 wks |
| 5 | Admin reporting | 20, 21 | Six reports + org-wide training records + manual archive | 2 wks |
| 6 | Groups admin + opportunities + group attestation | 6, 14–17 | Opportunity → QR registration → attendance → 30×30 attestation | 3 wks |
| 7 | OIDC cutover (CiviCRM) | 5, 18 | Login via CiviCRM only | 1 wk |

---

## 1. Technical decisions

### 1.1 Framework & UI stack
| Layer | Choice |
|---|---|
| Runtime | .NET 10 (LTS) |
| API style | Minimal APIs with vertical slices. Each `Features/<Feature>/` folder holds the endpoint, request/response DTOs, FluentValidation validator and handler. |
| Validation | FluentValidation |
| ORM / DB | EF Core + Npgsql, PostgreSQL |
| PDF | QuestPDF (Community licence) |
| Frontend | Vite + React + TypeScript SPA, TanStack Router (file-based, thin routes) + TanStack Query. Structure and conventions: `docs/web-architecture.md` |
| API client | ASP.NET OpenAPI document → Orval-generated TanStack Query hooks, one file per feature by API tag, plus Zod schemas (`npm run gen`) |
| UI/UX design | Claude Design design system → MUI theme + components synced via `/design-sync` |
| UI kit | MUI core + MUI X free (DataGrid, RichTreeView); dnd-kit for tree reordering; controlled inputs + `model/` hooks + generated Zod for forms (no form library); lucide-react icons; Lingui i18n (English + French); `react-signature-canvas` for signatures |
| Tests | xUnit (domain); xUnit + Testcontainers (integration); Vitest + Testing Library page test per web feature; Playwright smoke test per phase demo |

### 1.2 Project layout
```
src/
  SkillCert.AppHost/          Aspire orchestration (local dev)
  SkillCert.ServiceDefaults/  Aspire telemetry/health defaults
  SkillCert.Domain/           Entities, enums, pure services (currency, compliance, completion date). No EF.
  SkillCert.Infrastructure/   DbContext, migrations, S3 blob store, QuestPDF renderer, audit interceptor
  SkillCert.Api/              Features/<Feature>/ slices, authorization policies, nightly hosted job
  SkillCert.Migrator/         EF Core migration bundle (efbundle) image
web/                          Vite SPA; src/app (shell, routes), src/features/<name>, src/shared (see docs/web-architecture.md)
tests/
  SkillCert.Domain.Tests/
  SkillCert.Api.Tests/        Testcontainers Postgres
  e2e/                        Playwright
deploy/                       docker-compose.yml, Caddyfile, .env.example
.github/workflows/            ci.yml, deploy.yml
```

Cross-cutting rules:
- Inject `TimeProvider` everywhere, so expiry and nightly logic can be tested with a fake clock.
- Keep the domain `User` separate from the ASP.NET Identity user, linked by an `ExternalSubjectId`. This keeps the Phase 7 switch to OIDC small.
- Authorization uses additive capability policies (`IsAdministrator`, `HoldsClassification(Instructor)`, `IsAssignedInstructor(opportunity)`, …), not exclusive roles (spec §18).
- Administrators can never edit or delete CompetencyReview evidence. No endpoint exists for it.

### 1.3 UI/UX workflow: Claude Design
- A **SkillCert design-system project** in Claude Design is the source of truth for tokens (color, type, spacing, radius) and core components.
- `web/src/shared/lib/theme.ts` (`makeTheme(mode)`, light + dark) is generated from those tokens.
- Shared components are kept in sync with the project via `/design-sync`, one component at a time:
  - status chip
  - competency tree row
  - basket bar
  - signature pad
  - approval group card
- Each phase follows these steps:
  1. Design the phase's screens in Claude Design.
  2. Review them.
  3. Build against the approved designs.

  Every **Done when** includes "matches approved screens".

### 1.4 Devices & connectivity
- **Phone-first:** candidate, basket, signature, approval queue, attendance and attestation screens.
- **Desktop-first:** admin and reports. They must still be usable on a tablet.
- **Online only:** sign-off needs a connection, so ReviewedAt and revision resolution stay on the server. There is no service-worker caching.
- **Basic PWA manifest:** icons and full-screen launch only.
- Playwright candidate-flow tests use a mobile viewport (Pixel 7).

### 1.5 Data & time
- **Timestamps:** stored as UTC `timestamptz`.
- **Org timezone:** the server's local timezone governs:
  - the nightly run time
  - all date display
  - PDF sign-off dates

  All users share it.
  - Read it via an `IOrgClock` wrapper (over `TimeProvider` + `TimeZoneInfo.Local`).
  - Docker containers default to UTC, so `deploy/docker-compose.yml` sets `TZ` (e.g. `America/Denver`) on the api service.
  - Tests pin the zone explicitly and cover DST transitions.
- **Signatures:**
  - The client sends stroke points as JSON. The server validates them (point-count and size caps) and renders a canonical SVG.
  - `ReviewSignature.Data` = the SVG and `ContentType` = `image/svg+xml`, saved in the same transaction as the reviews.
  - Client-supplied SVG is never stored. The UI displays the stored SVG through `<img>`.
- **Seed data (dev + demo):**
  - The real AFA Skills Record hierarchy (see §2a).
  - Bogus-generated users across all groups and classifications.
  - Review history covering every state: current, expiring soon, expired, NotCompetent, pending, rejected, invalidated by a breaking revision.
  - Deterministic (fixed Bogus seed), so report tests have stable expected values.

### 1.6 Hosting & deployment
| Concern | Local | Hosted demo |
|---|---|---|
| Orchestration | .NET Aspire AppHost | Docker Compose on one VPS |
| Database | Postgres container (Aspire) | Postgres container (compose) |
| PDF blob store | RustFS container `rustfs/rustfs:1.0.1` (Aspire); the MinIO image was removed from Docker Hub | DigitalOcean Spaces (S3 API) |
| TLS / reverse proxy | n/a | Caddy (automatic Let's Encrypt), serves the SPA static files and proxies `/api` |
| Migrations | Migrator resource in Aspire | One-shot `migrator` service (efbundle); `api` depends on it with `service_completed_successfully` |
| Backups | n/a | None; demo data can be rebuilt from seed |

- **Environments:** local, plus one hosted demo.
- **CI/CD (GitHub Actions):**
  1. `ci.yml` (PRs and pushes): `dotnet test`, web lint + type-check + build, Playwright smoke tests.
  2. `deploy.yml` (push to `main`): build the `api`, `web` and `migrator` images and push them to GHCR tagged with the commit SHA.
  3. Deploy job: SSH to the VPS (key in Actions secrets), set the image tag, run `docker compose pull && docker compose up -d`. The VPS pulls the images; no binaries are copied.
- **Blob store:** `IBlobStore` on the AWS S3 SDK (works with RustFS and Spaces). Endpoint, bucket and keys come from config.

### 1.7 Authentication
- Phases 0–6: ASP.NET Identity with cookie auth and dev-seeded fake users.
- Phase 7: switch to external OIDC (CiviCRM) only. Local password login is disabled outside dev.

---

## 2. POC decisions that refine the spec
Record these in the next spec revision.

1. **Instructor sign-off on a candidate's device is trust-based.** The candidate picks a user with the Instructor classification, that Instructor signs on the candidate's device, and the review is immediately effective (`NotRequired`). The Instructor does not authenticate on that device. This replaces the spec §10 sentence "an automatic Instructor assessment requires the Instructor to authenticate".
   - *Why:* Instructor elevation is out of POC scope. Supervisor confirmation is the positive-attestation path for safety-critical skills; those competencies should permit only the Supervisor method.
   - Group attestation (spec §16) is unchanged: the Instructor is logged in on their own device.

2. **CompetencyRevision gains an optional `ShortTitle`.**
   - `Title` is the standalone full title, e.g. "Control major external hemorrhage – neck". It is used in the basket, approval queue, reports and history.
   - `ShortTitle` is the paper wording, e.g. "The neck". It is used only in the list tree and the training-record PDF, where the heading gives the context. When it is null, `Title` is shown instead.
   - Like `Title`, it is part of the revision and editable via Edit current revision.
3. **Training-record PDF evaluators use initials + a legend.**
   - Each row shows initials derived from the ReviewerName snapshot.
   - The last page lists each evaluator's name and initials, as on the paper form. If two evaluators share initials, they are disambiguated (e.g. JS, JS2).
   - Candidate identity is the name only: no CSP# and no New/Returning tick-box.

## 2a. AFA Skills Record source mapping
Source: `docs/AFA Skills Record - Revised v3.pdf`.
- **Size:** 63 competencies under 10 top-level headings (3–12). The tree is up to 3 levels deep: heading → sub-heading → competency.
- **Mixed levels:** some top-level headings hold competencies directly (3.1, 5.1, 8.1, 11.x, 12.x). Others hold sub-headings (4.1, 6.1, 9.1, 10.2). The unified node tree handles both.
- **Mixed code styles:** codes mix numeric and letter suffixes (4.1.1, 9.1a, 9.5.1). Order always comes from explicit `SortOrder`, never from the code.
- **Duplicate code fixed by re-lettering:** the paper has 10.2e twice. Seed codes from 10.2e onward are:

  | Code | Title |
  |---|---|
  | 10.2e | Femur |
  | 10.2f | Patella |
  | 10.2g | Lower leg |
  | 10.2h | Ankle or foot |
  | 10.2i | Boot and footwear removal |

  Paper order is kept.
- **Seed file:** `src/SkillCert.Infrastructure/Seed/afa-skills-record.json`. Drafted and approved on 4 Oct 2026. Each node holds code, ShortTitle (paper text), Title (standalone), and its children. Default `RecertificationDays` and permitted methods are set per competency for the demo.
- **PDF layout:** dark band for top-level headings, light band for sub-headings, Date + Initials columns. The paper prints 4.3 CPR in two columns; the PDF may use a single column.

## 3. Spec defaults still to finalize
| Item | Decide in |
|---|---|
| CSV method/resource column encoding; whether imported revisions publish immediately (spec §26) | Phase 4 |
| Registration windows, re-registering after Withdraw, attendance correction, cancelling an unused Draft (spec §15, §17) | Phase 6 |
| CiviCRM user provisioning (first login vs pre-created) and claim mapping | Phase 7 |

---

## 4. Phases

### Phase 0: Foundation + deploy pipeline (1.5 wks)
**Goal:** an empty app that runs locally and on the demo server, with login.

- [x] Create the solution and projects as in §1.2. Add Central Package Management and `Directory.Build.props` (nullable enabled, warnings as errors).
- [x] Aspire AppHost: Postgres, RustFS (S3), Migrator, Api. ServiceDefaults with health checks. (The Vite web resource is added with the web shell task.)
- [x] EF Core DbContext + first migration. `/health` endpoint. (Migrator applies migrations on start; snake_case naming; `dotnet ef` via local tool manifest.)
- [x] ASP.NET Identity (cookie auth). Dev seeder creates: 1 admin, 2 instructors, 2 supervisors, 10 candidates.
- [x] Domain `User` entity linked to the Identity user by `ExternalSubjectId`. `GET /api/me` returns the user and their capabilities. (ReviewerClassification and user assignments from spec §7 were brought forward from Phase 1 to provide Instructor/Supervisor capabilities.)
- [x] Web shell: Vite + React + TS, TanStack Router + Query, Orval generation from OpenAPI, login page, app shell, PWA manifest.
- [x] Claude Design (design-first; `/design-sync` waits until shared components exist in code):
  - [x] create the SkillCert design system and define the tokens: https://claude.ai/artifact/7AzLQcJz1v5Nk6J3zG7xor
  - [x] review the tokens and brand book in Claude Design
  - [x] generate `makeTheme(mode)` in `web/src/shared/lib/theme.ts` from `tokens.json` (copied to `web/src/shared/lib/design-tokens.json`)
  - moved to Phase 2: build the status chip in code, then run `/design-sync` to bring it into Claude Design
- [x] Template feature slice (endpoint + validator + handler + integration test) to copy for every later feature. (See `docs/api-architecture.md`; reference `Features/Auth/Login.cs`.)
- [x] Test projects: Domain.Tests, Api.Tests (Testcontainers), e2e Playwright login smoke test. (`tests/e2e`: phone + desktop projects; run against a live stack with `E2E_BASE_URL`.)
- [ ] `ci.yml`: build, test, lint, Playwright.
- [ ] `deploy/` files: compose (api, migrator, postgres, caddy, `TZ` set), Caddyfile, `.env.example`.
- [ ] `deploy.yml`: build images → GHCR → SSH `compose pull && up -d`. Provision the VPS (Docker, deploy user, SSH key, DNS) and the DO Spaces bucket.

**Done when:**
- `dotnet run --project src/SkillCert.AppHost` starts everything, and the Aspire dashboard shows all resources healthy.
- Each seeded role can log in and `/api/me` shows the correct capabilities.
- A push to `main` deploys to the demo URL over HTTPS.

### Phase 1: Domain core + currency engine (2 wks)
**Goal:** the data model and the currency rules, proven by tests. No UI.

- [ ] Entities + configurations:
  - Competency
  - CompetencyRevision (+ `ShortTitle`, RevisionResource, permitted review methods/classifications, `InvalidatesPreviousReviews`, `RecertificationDays`, PublishedAt)
  - CompetencyList, CompetencyListNode (unified heading/competency tree, explicit `SortOrder`)
- [ ] Entities: UserGroup, UserGroupMembership, GroupListAssignment, ReviewerClassification (+ `AffirmationPolicy`), UserReviewerClassification.
- [ ] Entities: CompetencyReview (all spec §8 fields), ReviewSignature.
- [ ] Constraints and indexes:
  - unique `(CompetencyListId, CompetencyId)` for competency nodes
  - unique normalized `Code`
  - parent node must be in the same list
  - competency nodes cannot have children
  - service-layer cycle check
  - index on `(CandidateUserId, CompetencyId, ReviewedAt)` for reviews
- [ ] Seed:
  - the AFA hierarchy, transcribed into `afa-skills-record.json` per §2a
  - groups New, Returning and Senior Patroller
  - classifications Instructor = Automatic, Supervisor = ReviewerConfirmation
  - the Bogus users and review history from §1.5
- [ ] `CurrencyEvaluator.Evaluate(reviews, revisions, asOf)` (pure). Returns Status, Reason, AchievedAt, ExpiresAt and HasPendingReview.
- [ ] Table-driven tests for each spec §12 rule:
  - evidence selection: only accepted reviews; ReviewedAt → CreatedAt → Id tie-break; no falling back to older favourable evidence
  - pending/rejected reviews never displace accepted evidence
  - expiry: Current strictly before ExpiresAt, Expired at/after; NULL interval never expires; day arithmetic (365 days ≠ 1 calendar year)
  - breaking revision invalidates from PublishedAt → NotCertified/RevisionInvalidated; late confirmation does not bypass it
  - NotCompetent removes currency; only a later Competent restores it; a pending NotCompetent has no effect
  - confirmation date: achieved Oct 4, confirmed Oct 18 → AchievedAt is Oct 4
- [ ] `RequiredListsResolver`: distinct union of lists across group memberships, and a distinct competency set across lists.

**Done when:** `dotnet test tests/SkillCert.Domain.Tests` is green with one named test per rule above, and seeded data loads in Aspire.

### Phase 2: Candidate sign-off + supervisor confirmation (2–3 wks)
**Goal:** a candidate signs off skills on their phone, and a supervisor confirms or rejects claims.

- [ ] Claude Design screens:
  - My lists tree
  - competency detail
  - basket + method picker + signature
  - review history
  - approval queue
- [ ] API: `GET /api/me/lists` returns the tree with currency per competency (batch evaluation, no N+1 queries).
- [ ] API: competency detail and my review history.
- [ ] UI: My lists tree with §13 presentation states, including Expiring Soon as display-only. Competency detail with resources and permitted methods.
- [ ] UI: sign-off basket (client state). Adding currently-valid competencies for reassessment is allowed.
- [ ] API: `POST /api/signoffs`. In one transaction it:
  - resolves the current revision
  - checks that every competency permits the selected method
  - rejects duplicate pairs
  - sets a single server ReviewedAt
  - stores one shared ReviewSignature
  - snapshots ReviewerName
- [ ] Methods:
  - Self: own user id, no signature
  - Peer: another registered user, no signature
  - Instructor: pick a classified user, signature required → NotRequired (trust-based, see §2)
  - Supervisor: pick a classified user, signature required → Pending
- [ ] Signature: canvas → stroke JSON → server renders SVG (§1.5).
- [ ] Approval queue API + UI:
  - shows only claims naming the current user
  - groups by candidate / ReviewedAt / signature
  - confirm or reject a whole group atomically; reject needs a reason
  - Rejected is final
  - an administrator cannot act for a reviewer
  - removing a classification keeps the right to decide existing claims
- [ ] Integration tests: method compatibility, duplicates, authorization (other users and admins get 403), one-time transitions, retrospective achievement date.
- [ ] Playwright: candidate signs off via Supervisor → supervisor confirms → candidate sees Current.

**Done when:**
- All 4 methods work end-to-end on a phone viewport and match the approved screens.
- The integration tests and Playwright smoke test pass.

### Phase 3: Training record PDF + nightly archival (2 wks)
**Goal:** a training record PDF that looks like the AFA paper record, and automatic archival when a user becomes compliant.

- [ ] `ComplianceEvaluator`: a list is compliant only when it is non-empty and every distinct competency is Current. Empty lists are non-compliant.
- [ ] `CompletionDateCalculator`: the start of the current period in which all competencies were Current at the same time, using evidence only. Tests include the spec §21 Sept 1 / Oct 4 removal case.
- [ ] QuestPDF renderer modelled on the AFA record (§2a layout):
  - candidate name, list title, completion date
  - full hierarchy, codes and ShortTitle (falling back to Title)
  - sign-off dates and evaluator initials, plus an evaluator legend on the last page (decision §2.3)
  - no expiry dates, signatures or resources
- [ ] Candidate "download current record" endpoint. Generated on the fly; it does not create an archive.
- [ ] `IBlobStore` (S3 SDK) + TrainingRecord entity (UserId, ListId, CompletionDate, GeneratedAt, BlobPath, Sha256Hash, Trigger). Unique index on (UserId, ListId, CompletionDate) where Trigger = ComplianceAchieved.
- [ ] Nightly hosted job (runs at a configured org-local time):
  - compliance checkpoint table
  - archive on first observed compliance or a NonCompliant → Compliant change
  - idempotent
  - a failed upload leaves no completed record and is retried next run
- [ ] Fake-clock tests: compliant 3 nights in a row → 1 archive; re-run the same night → no duplicate; non-compliant → compliant → 2nd archive; failed upload → retried.
- [ ] Playwright: candidate downloads their PDF.

**Done when:**
- The PDF is visually reviewed against the AFA record.
- All archival tests pass.
- Archives appear in RustFS locally and in Spaces on the demo.

### Phase 4: Admin configuration + audit + CSV import (3 wks)
**Goal:** administrators manage users, competencies, revisions and lists, and every change is audited.

- [ ] Claude Design screens: admin shell, users, competency editor (edit vs publish), list tree editor, CSV preview, audit viewer.
- [ ] Audit log: actor, timestamp, action, entity type/id, before/after JSON. Written by an interceptor or explicit service for configuration entities only.
- [ ] Users admin: list, view, deactivate (no delete). Assign/remove reviewer classifications.
- [ ] Competencies admin:
  - create
  - **Edit current revision** (editorial fix, audited, no invalidation)
  - **Publish new revision** (explicit breaking/non-breaking choice, recertification changes)
  - manage resources and permitted methods
  - deactivate
- [ ] List admin:
  - create/rename lists
  - tree editor (RichTreeView + dnd-kit) with headings, reordering and adding from the competency library
  - server enforces one occurrence per list and no cycles
- [ ] CSV import (spec §26):
  - upload → whole-file validation → preview of rows/warnings/errors
  - any error or existing-code conflict blocks the import
  - create competencies and initial revisions atomically
  - finalize the encoding (see §3)
- [ ] Audit viewer: filter by entity, actor and date.
- [ ] Tests: publishing breaking vs non-breaking changes currency as expected; an editorial edit doesn't; a CSV with one duplicate imports nothing; non-admins get 403.

**Done when:** every audited action appears in the viewer, the CSV tests pass, and the screens match the approved designs.

### Phase 5: Admin reporting (2 wks)
**Goal:** the six spec §20 reports, plus org-wide training records.

- [ ] Claude Design screens: report pages and the individual record view.
- [ ] Batch currency service: evaluate many users × competencies in a few queries, not one per row.
- [ ] Compliance Overview (organization and group level).
- [ ] User Compliance (missing/expired/non-current).
- [ ] Competency Compliance (currency rate per competency).
- [ ] Recertification Forecast (configurable window, e.g. 30/60/90 days).
- [ ] Pending Approvals (claims + responsible reviewer).
- [ ] Individual Training Record (admin view of any user) + manual archive (Trigger = AdministratorManual).
- [ ] Tests compare each report with hand-computed values on the fixed seed. Performance check with 500 seeded users: every report responds in under 2 s.

**Done when:** all report tests pass, the performance check passes, and the screens match the approved designs.

### Phase 6: Groups admin + opportunities + group attestation (3 wks)
**Goal:** scheduled events from registration through attendance to classroom-scale attestation.

- [ ] Claude Design screens:
  - groups admin
  - opportunity listing and detail
  - QR check-in
  - instructor participant/attendance view
  - attestation grid
- [ ] Groups admin: CRUD, user membership and list assignment, all audited. Groups are seed-only before this phase.
- [ ] Finalize the §3 Phase 6 defaults.
- [ ] Opportunity entity:
  - title, description, location, StartsAt/EndsAt, capacity, Status, AudienceMode
  - competencies (distinct, can be copied from a list)
  - assigned instructors, audience groups
  - never hard-deleted
- [ ] Lifecycle: Draft → Open → InProgress → Completed, and Open → Cancelled. Explicit transitions only; nothing is inferred from the clock. Completed is read-only for instructors.
- [ ] Registration:
  - from the listing, or via a QR with a signed opaque token
  - audience and capacity checked in one transaction; Registered + Attended count toward capacity
  - Withdraw
  - one registration per user per opportunity
- [ ] Instructor views: assigned opportunities, participants, mark attendance (admin can override), show the QR, mark Completed.
- [ ] Group attestation:
  - grid of attended candidates × competencies
  - default Competent; Not Assessed creates no review; Not Competent must be chosen explicitly
  - one transaction, shared ReviewedAt + signature + OpportunityId
  - records the Instructor classification held at that moment
- [ ] Tests: lifecycle transitions; concurrent registrations never exceed capacity; a 30×30 attestation creates 900 reviews in under 5 s.
- [ ] Playwright: register via QR → instructor marks attended → attests → candidate sees Current.

**Done when:** all tests pass and the screens match the approved designs.

### Phase 7: OIDC cutover to CiviCRM (1 wk)
**Goal:** users log in through CiviCRM only.

- [ ] Finalize provisioning and claim mapping (§3).
- [ ] Add OIDC (authorization code + PKCE). Map `sub` to `User.ExternalSubjectId`. Provision on first login or match pre-created users.
- [ ] Turn local password login off outside dev. Keep dev seeding for Aspire.
- [ ] Configure demo secrets (client id/secret, authority) in Actions secrets and the VPS `.env`.

**Done when:** all Playwright smoke tests pass on the demo while logged in through CiviCRM.

---

## 5. Spec traceability
| Spec § | Topic | Phase |
|---|---|---|
| 1 | Purpose | All |
| 2 | Competencies | 1, 4 |
| 3 | Competency revisions | 1, 4 |
| 4 | Competency lists | 1, 4 |
| 5 | Users | 0, 4, 7 |
| 6 | User groups | 1 (seed), 6 (admin) |
| 7 | Reviewer classifications | 1, 4 |
| 8 | Competency reviews | 1, 2 |
| 9 | Sign-off basket | 2 |
| 10 | Reviewer identity & signature | 2 (see decision §2) |
| 11 | Reviewer confirmation | 2 |
| 12 | Competency currency | 1 |
| 13 | Candidate experience | 2, 3, 6 |
| 14 | Opportunities | 6 |
| 15 | Registration & attendance | 6 |
| 16 | Group attestation | 6 |
| 17 | Opportunity lifecycle | 6 |
| 18 | Authorization | 0, 2, 4, 6 |
| 19 | Administration | 4, 6 |
| 20 | Reporting | 5 |
| 21 | Training record PDF | 3, 5 |
| 22 | Automatic archival | 3 |
| 23 | Audit principles | 4 |
| 24 | Out of scope | Not built |
| 25 | Domain at a glance | 1 |
| 26 | CSV competency import | 4 |
