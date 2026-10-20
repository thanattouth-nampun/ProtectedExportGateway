# 13 — UX/UI specification v4.2

> ADR-014 access update (20 October 2026): [Self-service Member workspaces](member-self-service-workspace.md) supersedes Publisher-only/pre-enrolled-owner empty-draft access below. Every verified internal tenant Member gets own private drafts; source RMS-owner mapping, reviewed recipients, scan/release and report/auditor controls remain. Approved tenant claim/enrollment rollout and live acceptance are tracked separately in ADR-014.

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

Decision: 19 October 2026, ADR-012. The user removed 3D from the project. The product uses a searchable HTML document list with owner filtering and readable details/actions. React/TypeScript, .NET BFF, English UI, Inter and the approved theme remain. Old renderer evidence is historical and does not define current acceptance.

## Navigation and authorization

| Screen | Access |
|---|---|
| `/library` | LibraryReader plus current publication/ACL; authorized records only |
| `/my-documents` | Own document metadata; mutations require DocumentPublisher and ownership |
| `/reports` | Report-specific roles; no implied payroll access from auditor/owner |
| `/my-exports` | Requester's jobs/download history |
| `/audit` | ExportAuditor metadata only; no download of other users' artifacts |
| `/sign-in` | Explicit Microsoft Entra login |

Server authorization remains authoritative for every request. Do not fetch a global dataset and hide unauthorized records in the browser. Counts, owner options and search results derive only from authorized records/pages. No production persona switch or fake artifact.

## Library and details

Folder-navigation experiment (19 October 2026): the user approved a tree/list/details preview. See [scope and behavior](folder-navigation-preview.md). Folder navigation is an authorized metadata projection; Search can cross folders. SharePoint/OneDrive connectivity remains future work. Preserve the full HTML list and all copy/intake/release controls.

- Reader landing is the searchable document list. No renderer, view toggle, camera controls or GPU dependency.
- Show title, business owner, supplied filename, current published version, classification expectation and allowed action. Search by title/filename and filter by business owner; server-authorized bounded pagination is required when connected to the store.
- Use selected-row styling plus an HTML detail panel on wide screens; stack list/details or use an accessible sheet on narrow screens. Selection, forms and job state belong to authorized application state.
- Details distinguish business owner, requester/recipient and processor/protection owner. Show purpose and intended edit/save capability; label display alone is not protection proof.
- No workbook content preview or source/quarantine URL. Preserve supplied metadata/tenant label names and encode them as text.
- Request my copy sends document ID/current source version only. Recipient/label/rights/owner are server-owned. No required confirmation for this reversible request; idempotency prevents duplicates.
- No-result, empty, unauthorized, dependency failure and expired states provide clear recovery without leaking private metadata.

## Owner lifecycle

My documents shows current published version separately from the latest upload/intake state. A pending or rejected v2 does not replace published v1.

Upload wizard has three persistent, keyboard-accessible steps: file/details, same-tenant requester allowlist, review/submit. Title/purpose, one synthetic unprotected XLSX and explicit limits appear before submission (10 MiB file,12 MiB request). Owner derives from session. No arbitrary recipient email, label, path or ownership override.

Native file picker must work without drag/drop. Inline validation and a linked error summary preserve entered values and focus the first invalid field. Back retains values. Unsaved cancel requires discard confirmation only when data would be lost. Show measured upload progress only; scan/structural validation use actual states without invented percentage/ETA.

Upload success means AwaitingValidation, not Published. Publish requires real Eligible evidence and server revalidation. No scan bypass. Replacement publish confirmation identifies exact old/new versions and explains that older portal jobs/downloads stop. If-Match conflicts require reload/review rather than overwrite.

Withdraw confirmation identifies document/version and explains that future portal copy requests/downloads stop while downloaded files are not automatically revoked. Focus Cancel initially; support Escape, focus trap, inert background and focus return. Owner metadata authority never grants access to another requester's artifact.

## Reports, jobs and audit

Reports show only authorized headcount-summary/payroll-detail options, synthetic data purpose/dataset/rule and allowed periods. Classification rules are server-owned.

Jobs show actual Queued/Preparing/Labeling/Verifying/Publishing/Ready/Failed/RetryPending/Expired/Simulated states, safe reasons, correlation reference and local time with timezone. Display Asia/Bangkok for user-facing dates. No fabricated security success, progress or ETA. Simulated jobs have no download action. Ready downloads still require current server authorization, exact verified artifact/version/hash and TTL.

Evidence separates expected vs observed label/protection/recipient/rights, source owner vs processor vs recipient and metadata observation vs Office access test. Auditor sees redacted metadata, never credentials, raw policy/scanner dumps or unrestricted downloads.

## English copy, typography and theme

Application-owned visible/accessibility text is English; preserve supplied filenames/titles/names and tenant labels. Use sentence case: Document library, My documents, Reports, My downloads, Audit log; Add document, Upload file, Publish, Withdraw, Request my copy, Download my copy, Cancel, Retry. Prefer Edit and save rather than claiming built-in Editor equivalence.

Self-host official Inter WOFF2 400/500/600 with SIL OFL notice, font-display:swap and `Inter, system-ui, -apple-system, Segoe UI, Arial, sans-serif`. Body 16 px/1.5; controls/tables at least 14 px; headings 20–32 px. Use tabular numerals for aligned numeric columns.

| Semantic token | Value |
|---|---|
| background | #F7F8F5 |
| surface | #FFFFFF |
| foreground | #202923 |
| muted-foreground | #536359 |
| primary / primary-foreground | #24634B / #FFFFFF |
| primary-hover | #1B503C |
| selected-background / selected-foreground | #E5F0E9 / #24634B |
| border / control-border | #DCE3DD / #738578 |
| success-foreground | #166544 |
| warning-foreground | #805600 |
| error-foreground | #A72C35 |

Use neutral surfaces and green for selection/actions. Decorative border alone is not an input boundary. Focus uses a visible primary outline with offset. Status needs text/icon as well as color; green is not proof of encryption. Verify hover/focus/selected/disabled/loading/error/destructive states. Fixed light theme; no dark-mode promise.

## Accessibility, responsiveness and acceptance

Target WCAG 2.2 AA: normal text 4.5:1, large/non-text relevant boundaries 3:1, visible unobscured focus, semantic labels/headers and preferred 44 px controls. Polite live status avoids repeated poll announcements. Tooltips are supplementary. Reduced motion removes decorative transitions; no task depends on animation.

Wide: navigation+list+details; medium: compact navigation and stacked details; narrow 390 px: wrapped cards/forms, reachable controls, no page-level horizontal clipping. Verify native 200% zoom/reflow, fallback fonts and slow/error states. No accessibility certification is implied by design or automated tests.

| ID | Acceptance |
|---|---|
| X01 | Reader finds an authorized document and requests a copy using search/list; record real task observations |
| X02 | Reader distinguishes business owner from copy recipient |
| X03 | Owner completes file/allowlist/review; inputs/errors retained; submission is not publication |
| X04 | Scan, publish and export readiness are distinct |
| X05 | v1 published while v2 pending is clear |
| X06 | 390 px and 200% zoom/reflow keep all actions reachable |
| X07 | Keyboard/file picker/dialog focus/Escape/return work |
| X08 | Reduced motion and UI/data-load failures preserve usable list/forms/job recovery |
| X09 | Empty/unauthorized/error/expired/unknown states are accurate and safe |
| X10 | Fixture harness stays explicitly synthetic and out of release assets |
| X11 | Only real server-authorized Ready jobs offer download; simulation never releases |
| X12 | Component source/license/dependencies/keyboard/motion/contrast reviewed before reuse |

G7 retains compatible build, list/details, session/CSRF/routes/cache, accessibility/load/device and same-origin Azure regression. Renderer/FPS/context-loss acceptance is retired by ADR-012. Full live G7 and a 3–5 participant usability pilot remain untested.
