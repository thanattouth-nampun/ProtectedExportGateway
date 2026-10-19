# Start here — Protected Export Gateway v4.3

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
