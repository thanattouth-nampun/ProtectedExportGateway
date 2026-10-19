# Repository instructions — v4.2

Read README.md, docs/01–15 and prototype/README.md. ADR-012 supersedes the former renderer requirement: the user removed 3D from the project. Adopt React/TypeScript/Vite with a searchable HTML list/detail workspace with existing .NET API/BFF/native worker and preserve all functional/security/Azure controls. Do not drop modules or expand to AI/unrestricted uploads/decryption/approval/multi-tenancy/catalog. English-only application UI and identifiers; planning documents may remain Thai. Use self-hosted Inter with system fallbacks per docs/13. Translate legacy Thai UI examples; preserve user-supplied metadata and tenant label names.

## First work

Follow docs/07 dependency order. Validate native SDK identity, user-defined protection, recipient/rights readback, crossed-copy client tests and live intake/scanner gate G6 before claiming v3 complete. Missing tenant/subscription blocks live gates, not independent implementation/IaC preparation. Record versions and exact API semantics; never invent successful permissions, licensing, Clean verdicts or Azure deployment.

## Invariants

- DocumentPublisher creates owner-bound drafts and uploads bounded synthetic unprotected XLSX; publisher+owner required for mutations. Seed fixtures pass same validation gate; no protected-source decryption workflow.
- Quarantine first, trusted clean scan then bounded OOXML validation then new source-version promotion; owner publishes explicitly. Fake/unknown/error/remediated scans never make live eligible/published sources. Scanner exit0 alone is not Clean.
- Do not elevate web/MIP worker to admin for scanning; validate a narrow trusted helper with path/token allowlist and fixed commands. Do not execute workbook formulas/Office or add AV exclusions to force success.
- Metadata/ACL/version/publish/withdraw use optimistic concurrency and transactional audit. New Draft does not replace current published version; latest published/ACL/source status checked at issuance and download.
- Use File SDK in worker. Direct Policy/Protection SDK is not initially required.
- Distinguish business owner, requester, processor/RMS owner and authorized recipient. Do not delegate protection ownership to downloader by default.
- Per-user document copy contains only the verified requester as ordinary recipient. No broad group/domain grants or accumulated users. Label can be shared; publishing rights are per copy.
- Exact edit/save profile is custom and client-tested. Built-in Editor can include Copy/Export; never claim equivalent isolation without inspecting rights.
- Source and identity come from trusted server records bound to tid/oid. Reject caller-supplied recipients/labels/paths/owner.
- Idempotency/cache keys include requester, tenant, source immutable version/hash and policy/rights version. Artifacts cannot be reused across recipients.
- Ready requires independent output evidence for label, protection and recipients/rights for per-user copies; unknown is failure. Bind to SHA-256 and immutable blob version.
- Simulated jobs never release downloadable artifacts. Metadata-only tests never prove client denial.
- Authorization/ACL/source availability/expiry checked again at download; no direct source URLs/public blobs/raw fallback.
- Durable Azure SQL jobs, transactions, leases/fencing and orphan recovery; one worker baseline. Blob publication is not atomic with SQL: candidate object is inaccessible until committed Ready binding.
- Web App Service has quarantine-upload scope only plus export-read; no source read/write or export-write. Ingestion worker has quarantine read/cleanup, reviewed source-promotion read/write scope and export write/delete as required. Source promotion uses unique create-only keys and pinned versions; Azure write RBAC alone does not enforce write-once. Separate managed identities and MIP certificate identity; document same-VM trust limits.
- Use managed identity for Azure data services; do not assume it replaces MIP OAuth credential flow. Never log certificate private keys, SDK caches or tokens.
- No Azure resource creation/deployment/billing activation without concrete target/cost authorization. Prepare code/IaC and preview first; asking about hosting is authorization to design it.

## UX/UI requirements

Follow docs/13–15 and ADR-012: Reader uses a searchable HTML document list with owner filtering, selection and accessible detail/actions. Publisher has own upload/version/publish flows; Auditor metadata only. Do not reintroduce a 3D renderer, camera, WebGL dependencies or decorative security counters.

The user approved a folder-tree/list/details UI preview. Folder navigation is an optional projection of authorized metadata and must not grant file access, infer business ownership or introduce a SharePoint/OneDrive connector. The synthetic hierarchy and disconnected-source examples belong only to the development harness. Future server folder/connector contracts require explicit scoped design; preserve global search and the same release/intake controls. See docs/folder-navigation-preview.md.

21st.dev is a React component source catalog; review original source/licenses/dependencies/accessibility and consistent design before reuse. One React UI, no dual Razor/island baseline. Existing prototype is legacy buildless design reference, not the production React application. Production must not include fixture persona switch/simulated steps/fake downloadable artifact.

Build frontend and .NET server as one web release/origin. Server owns OIDC/HttpOnly cookie/CSRF; no browser MIP credentials or Entra tokens in persistent JS storage. Clear authorized query/selection state across identity boundaries, generate DTOs from OpenAPI and never let SPA fallback intercept /api or /auth errors. Node is build-time, not a required separate production server.

Implement keyboard focus/labels/dialog behavior, responsive layouts/reflow, reduced motion, contrast and clear unknown/error/expired states per X01–X12. Do not claim a prototype or a library component is accessibility-certified/security-tested without evidence.

## Suggested layout v4

```text
src/Gateway.Web/
src/Gateway.Client/             # React/TS/Vite, list/detail workspace and typed clients
src/Gateway.Application/
src/Gateway.Domain/
src/Gateway.Infrastructure/
src/Gateway.Worker/
tools/MipFeasibility/
tools/SeedDocuments/
tests/Gateway.UnitTests/
tests/Gateway.IntegrationTests/
tests/Gateway.LiveMipTests/
infra/bicep/
prototype/dist/                 # reference only, no production auth/SDK
docs/
```

Pin SDK/runtime/packages after G1. Local DB uses SQL Server-compatible engine; cloud uses Azure SQL. Runtime dirs/caches/certs/databases/traces/terraform states or equivalent deployment secrets must be ignored. Keep sanitized config only. Bicep default parameters must not contain credentials or auto-deploy anything.

## Completion

Meaningful tests cover modules/intake I01–I16, backend release/authorization/recovery, search/list/detail behavior, CSRF/session/API-route behavior, identity-cache clearance, UI failures, responsive layouts and device performance. Record actual commands/results and G0–G7 evidence. Static prototype, package installation or IaC validation alone proves neither full workflow acceptance nor live deployment/MIP behavior. Do not claim native audit/accessibility certification/production readiness without evidence.

Approved theme: warm-white background #F7F8F5, white surfaces #FFFFFF, charcoal text #202923, forest-green primary #24634B and soft-mint selection #E5F0E9. Apply semantic tokens and state rules from docs/13; preserve English-only UI and Inter typography. Earlier blue-primary proposals are superseded.
