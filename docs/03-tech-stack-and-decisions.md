# 03 — Technology and decisions v4.2

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

Selections define the current baseline. Local validation is recorded in the current evidence; live/deployment acceptance remains incomplete.

| Layer | Selection | Validation |
|---|---|---|
| Runtime | .NET 10 LTS, Windows x64 MIP worker | exact SDK/native/OS combo in G1 and VM G5 |
| Frontend | React + TypeScript + Vite, React Router, Tailwind/reviewed primitives | same-origin typed API/query/form conventions; exact supported versions pinned in G7 |
| Server state/forms | TanStack Query + React Hook Form/Zod | authorized cache shared between list/details; server validation remains authoritative |
| Workspace | HTML list, search/owner filter and details | Keyboard/reflow/identity-cache/error behavior in G7; renderer retired by ADR-012 |
| Web backend/BFF | ASP.NET Core .NET 10 API/session + static React build hosting | cookie/OIDC/CSRF server-side, same-origin JSON/polling; no client MIP/Entra token storage |
| Sign-in | Microsoft.Identity.Web / OIDC | single tenant, app roles + document ACL, no tokens in localStorage |
| Processor auth | MSAL.NET + certificate-backed MIP application | actual flow/permissions/ownership proven in G1; no automatic delegation to requester |
| SDK | Microsoft.InformationProtection.File | apply labels + per-copy custom protection through File SDK |
| Workbook | ClosedXML | synthetic XLSX only, no Excel COM |
| Cloud web host | Azure App Service Windows | runtime/publish/auth/outbound private connectivity in G5; MIP not hosted here |
| Cloud worker | Azure Windows VM + supervised .NET Worker | controlled native dependencies, one active attempt |
| Persistence | EF Core SQL Server provider → Azure SQL Database | DB durable queue/transactions/fencing; SQL-compatible local dev |
| Files | Azure Blob Storage, private source/export containers | managed identity, pinned versions, no public/SAS download |
| Intake | bounded ASP.NET multipart stream → private quarantine | publisher/owner authorization and measured bytes/hash, no execute/preview |
| Malware scan | candidate Windows Defender Antivirus via scoped helper | G6 must prove clean/detection/remediation/unknown semantics and safe privileges |
| File validation | bounded OOXML/ZIP inspection after scan | safe XML parsing, no formula calculation/Office; no protected/active-content sources |
| Secrets | Azure Key Vault | scoped MI retrieval of separate web and MIP credentials; certificate public portion alone is insufficient |
| Azure access | Azure.Identity / Microsoft.Data.SqlClient / Azure.Storage.Blobs | actual data-plane RBAC/SQL users tested |
| Observability | Azure Monitor/Application Insights | redaction/sampling/cost caps and application audit separated |
| Infrastructure | Bicep templates + parameter examples | validate/what-if and cost review before provisioning |
| Tests | xUnit + HTTP/DB/process integration + manual Excel | live SDK/client/cloud evidence separate |
| Frontend build/tests | supported Node LTS for CI/build only; TypeScript/component/browser tests | one production web artifact; no mandatory Node server; G7 list/session/error/reflow evidence |

## ADR-001 — Two modules share an export pipeline

Reports remain context-driven. Document Library is a second source kind/profile with Owner upload, metadata/ACL/version management and publish/withdraw. Approval/catalog/AI and unrestricted file types remain out of scope. Seed fixtures pass the same intake contract.

## ADR-002 — Per-copy UDP protection

Use the same UDP sensitivity label across copies, with independently generated recipient rights for each job. Do not modify tenant label recipient configuration per click. No file/descriptor cache shared across recipients. See [docs/10](10-per-user-document-download.md).

## ADR-003 — Restricted edit/save profile

User's Editor goal is opening/editing/saving. Proposed rights encoding is VIEW + DOCEDIT + EDIT; exclude EXPORT, OWNER, EXTRACT and other unnecessary rights. Do not silently map to built-in Editor, which can contain broader rights. SDK constant names, label compatibility and client behavior are G1/G2 tests; if unsupported, surface the conflict rather than granting broader rights to pass.

## ADR-004 — Ownership and identity

Baseline candidate processor uses app-only service ownership. Document business owner is preserved as application metadata, not automatically recipient or RMS owner. Ordinary recipients are limited to requester in DocumentCopy baseline; operational/service rights documented separately. Do not use DelegatedUserEmail=requester unless an approved change explicitly accepts requester becoming protection owner.

Candidate policy/content permissions remain tenant policy read and content writer as documented, but UDP apply/inspect must be proven. Do not add Content.SuperUser merely for convenient inspection. Azure managed identity provides Blob/SQL/Key Vault access; it does not automatically satisfy the MIP application's OAuth requirements.

## ADR-005 — App Service web + VM worker

Separating native SDK worker on a VM avoids assuming a PaaS sandbox supports every dependency. This is a control/simplicity tradeoff with VM patching and ongoing cost, not a vendor statement that MIP on App Service is unsupported. Direct worker PaaS hosting requires a separate spike before changing baseline.

## ADR-006 — Azure SQL and Blob are authoritative

Distributed cloud components cannot share a local SQLite DB or App Service filesystem safely as baseline. SQL persists jobs/state/audit; Blob stores sources and final artifacts. Publication uses private candidate then transactional DB binding; compensating orphan cleanup handles cross-service failure.

No separate message broker required for the small demo. Later multi-worker scaling would require explicit load/lease correctness review.

## ADR-007 — Identity mapping is explicit

Authorize by tid/oid, never email alone. Recipient address must come from validated tenant claims/directory mapping bound to that identity, preferably operator-provisioned mapping for the demo. Missing/ambiguous/stale mapping fails closed; browser cannot choose it. Graph-based resolution, if later needed, requires a separately reviewed permission, not blanket directory access.

## ADR-008 — Azure infrastructure is designed before provisioned

IaC can be prepared locally. Subscription, region, SKU availability, cost ceiling, ingress audience and operator access path must be concrete before provisioning. No charge estimate is asserted until priced for selected parameters. App Service plan, VM/disks, SQL, private endpoints, logs and retained blob versions all contribute cost.

## ADR-009 — Quarantine and explicit publishing

Source ingestion is part of v3: owner derived from session, Publisher+owner mutations, private quarantine, trusted malware verdict, bounded OOXML validation, source promotion and explicit publish. No clean-by-extension or seed bypass. Version/ACL changes use If-Match and transactional audit. Current publication/version rechecked at release/download.

Candidate VM AV helper reuses current compute rather than assuming Defender for Storage is already enabled or adding unused cloud services. Validate endpoint permissions/platform/signature freshness and result semantics in G6; exit0 alone is insufficient. Optional Defender for Storage requires separate ADR, actual pricing and authoritative verdict binding. Details in docs/12.

## Versions and supply chain v3

Pin stable exact packages/global.json/lockfiles after G1; verify publishers/licenses/vulnerabilities. Source S04 runtime claims do not prove the full .NET/native stack. Bicep API versions also verified at implementation. Do not copy obsolete authentication examples or deploy a shorter-lived runtime just because a sample uses it.

All source IDs refer to [research register](09-research-and-open-questions.md). Revised decisions supersede the prior local-only hosting design; no legacy SQLite cloud instructions apply.

## ADR-010 / ADR-011 — Historical frontend decisions

The Razor/static and subsequent React/renderer choices are historical. ADR-012 retains React and removes the renderer. [Historical assessment](history/15-v4.1-stack-assessment.md).

## ADR-012 — Searchable HTML workspace; remove 3D

The user explicitly removed 3D on 19 October 2026 because it does not fit this project. Keep React/TypeScript/Vite/router/query/forms/Tailwind and the .NET BFF/worker. Use an accessible searchable HTML document list with owner filtering, selected details, upload/version/publish forms and job/audit views. Remove Three/R3F/Drei, camera controls, WebGL assets/runtime code and renderer-only checks.

Same-origin cookie/OIDC/CSRF, generated DTOs, identity-cache clearance and all report/library/MIP/scanner/Azure controls remain mandatory. T31 is retired; G7 is frontend/session/accessibility/load/cloud regression without renderer/FPS gates. Retain old evidence as historical; removal does not convert any live gate into PASS. See [current assessment](15-long-term-stack-assessment.md).
