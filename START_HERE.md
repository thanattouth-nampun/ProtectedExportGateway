# Start here — Protected Export Gateway v4.3

Latest26 October T24 increment: [native output/health/error controls](docs/evidence/2026-10-26-scanner-native-controls.md). 145 unit tests/18 offline CLI checks passed. Real missing-target scan returned2/error; real benign scan matched exact text and bracketed health. Human-approved EICAR attempt failed during vendor fetch, no ZIP written/custom control scan/detection proof, no retry/bypass. T24/G6 still incomplete, no trusted Clean or release. [Exact reviewed scope](docs/scanner-controlled-test-review.md).

26 October next checkpoint: [Owner-positive/scanner callability](docs/scanner-feasibility-checkpoint.md), [actual evidence](docs/evidence/2026-10-26-owner-positive-scanner.md). Original Owner uploaded the synthetic file through real Entra/browser session; fresh SDK returned OWNER_ONLY_OBSERVED_NOT_ELIGIBLE. Ordinary-user Defender benign custom scan reported no threats with unchanged hash, but trusted Clean remains unestablished. 124 unit tests +16 offline CLI checks passed this increment. No content extraction, persisted eligibility or release; G6 still not passed.

26 October checkpoint: [Read-only quarantine ownership probe](docs/quarantine-ownership-checkpoint.md), [actual evidence](docs/evidence/2026-10-26-quarantine-ownership.md). 203 tests passed; live SDK rejected the existing B-uploaded envelope with RMS_OWNER_MISMATCH. SQL/web remains AwaitingValidation, no Publish/download. Positive owner intake and T24/T25/G6 content inspection/scanner/promotion remain pending. No tenant/Azure changes.

21 October checkpoint: [Development-only local quarantine receipt](docs/quarantine-upload-checkpoint.md) and [actual evidence](docs/evidence/2026-10-21-quarantine-upload.md). Raw synthetic envelope receipt/hash/status works; source ownership/scanner/protected validation/Azure Blob binding and production upload eligibility remain unproven. Next T24/T25/G6; no Publish/download release.

Latest access decision: [ADR-014 self-service Member workspaces](docs/member-self-service-workspace.md). All verified internal tenant Members get own private drafts without Publisher enrollment. This does not relax RMS-owner/scan/release gates. The human approved the exact Entra member-claim/enrollment changes and the new local artifact is active; fresh Member login/acceptance is tracked separately from the 183-test local checkpoint.

Latest scope: [ADR-013 standalone owner-only source](docs/standalone-owner-only-mvp.md). Defer SharePoint/OneDrive/content connectors; retain Entra/MIP/Azure and both report/library modules. The owner controls a portal requester ACL independently from source RMS rights. New copies must retain the original RMS owner, plus exactly the verified requester as ordinary editor-profile recipient. Login alone never authorizes download.

Next dependency is the guarded owner-only-source spike, not more Library screens. See [operator review packet](docs/owner-only-spike-runbook.md). Local preflight/rule tests are not live evidence. Existing unprotected parser/intake, service-owner snapshots and SQL Ready prohibition remain legacy fail-closed foundations until protected intake/ownership/scan/release are implemented and tested; the older descriptions below are superseded where they conflict.

Read README.md, AGENTS.md and docs/01–15 plus prototype/README.md. Use the current repository directly; docs/07 defines dependencies and evidence gates.

Latest user decision: ADR-012 removes 3D. Keep React/TypeScript/Vite, React Router, Tailwind/reviewed components, TanStack Query, React Hook Form/Zod, .NET 10 API/BFF and Windows MIP File SDK worker. Build React assets into the same .NET web release/origin. Node remains build/CI only.

Reader workspace is a searchable authorized HTML document list with owner filter, selected details and copy actions. Publisher owns upload/metadata/ACL/version/publish/withdraw forms; Auditor sees metadata only. No renderer/camera/WebGL requirement or T31 dependency remains. Historical renderer tests are not current deliverables. G7 still validates frontend/session/accessibility/load/cloud behavior.

Keep English application UI, official self-hosted Inter 400/500/600/system fallback, warm-white#F7F8F5, white#FFFFFF, charcoal#202923, forest-green#24634Band mint selection#E5F0E9. Preserve supplied metadata and tenant label names.

Preserve both modules: synthetic HR context-based exports and Owner-published library copies. Quarantine→trusted clean scan→bounded OOXML validation→immutable source promotion→explicit Owner publish. Publisher+owner and optimistic concurrency/transactional audit are mandatory. New drafts do not replace current published versions. Withdraw stops future portal requests/downloads, not already downloaded file rights.

Keep requester/recipient/business owner/protection owner distinct. Per-copy recipient is the verified requester alone; no shared-label recipient mutation or accumulated users. Exact VIEW/DOCEDIT/EDIT rights, independent output label/protection/recipient/owner readback and Office edit/save/cross-denial require live evidence. Ready binds hash and exact immutable Blob version. Recheck current tenant/requester/role/ACL/publication/version/expiry at download. Never release simulated/raw/plaintext artifacts.

Server owns OIDC/HttpOnly cookie/CSRF; no persistent browser OAuth/MIP tokens. Generate DTOs from OpenAPI. Reserve API/auth routes from SPA fallback; clear query/selection/forms/in-flight state across identity changes.

Current code includes the [T06 durable SQL checkpoint](docs/sql-queue-checkpoint.md): EF migrations, isolated real LocalDB tests, idempotency, leases/fencing, attempts/recovery and transactional audit, alongside the native-load probe and BFF/HTML workspace. M0/M1/live MVP remain incomplete. Continue authenticated source/ACL/owner APIs, bounded worker/intake and full role flows. Missing tenant/scanner settings block live gates, not independent implementation. Fake/unknown/remediated scan never publishes.

Run scripts/verify.ps 1 for the local checkpoint; read docs/developer-setup.md and current evidence. Prepare infrastructure/config/runbooks locally. Before tenant changes, admin consent, chargeable provisioning or deployment, prepare exact target/change/cost/rollback for explicit owner authorization. No secrets through chat. Report implementation, simulation and live evidence separately.
