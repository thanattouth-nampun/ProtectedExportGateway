# 15 — Long-term stack assessment v4.2 / ADR-012

Decision: 19 October 2026. The user removed 3D because it does not fit this project. This supersedes ADR-011's renderer requirement while retaining its React/.NET same-origin architecture. The [v4.1 assessment](history/15-v4.1-stack-assessment.md) and earlier evidence are historical.

## Selected architecture

React/TypeScript/Vite + React Router/Tailwind, TanStack Query, React Hook Form/Zod and selectively reviewed shadcn/21st components. The Reader uses a searchable HTML list with owner filter and selected detail/actions; Publisher forms and Auditor metadata use the same app/session/contracts.

The subsequent user-approved [folder-navigation experiment](folder-navigation-preview.md) adds an optional HTML tree and breadcrumbs around that list. It does not change the native/security/backend stack or add a remote source connector. Development fixture paths must not become inferred production source records.

Keep ASP.NET Core .NET 10 API/BFF and the Windows native MIP File SDK worker. Azure SQL/private Blob/Key Vault/Monitor and all business/security/native gates remain. Build static React assets into the App Service .NET web release/origin. Node is build/CI tooling, not a second production server.

Remove Three, React Three Fiber, Drei, their renderer-only transitive packages, camera/scene code and GPU-specific tests/budgets. No replacement illustration or animation is required. This is a scope decision, not a claim of measured usability improvement.

## Assessment and boundaries

The HTML list/detail workspace directly supports finding documents, understanding ownership, submitting bounded files and reviewing job/evidence state. Removing the renderer eliminates its runtime, assets, camera state and GPU compatibility/maintenance work. Actual bundle deltas are recorded in new evidence; do not infer unmeasured load-time or business ROI figures.

React remains useful for routing/query/form conventions and interactive Owner/job workflows. There is no reason to introduce Razor islands, Next/Node backend or change the native worker solely because the renderer was removed. Component sources still need individual license/dependency/accessibility review.

The browser receives only authorized metadata. Search/filter/owner options/counts/pagination must not expose hidden documents. List selection/details/forms/job status share requester-scoped authorized state. No raw source URLs, browser MIP credentials, persistent OAuth tokens, offline mutation replay or fixture persona switch.

## Same-origin release/session

- One .NET release serves static assets and API/auth. Private runtime/certificates/files/caches stay outside webroot.
- .NET owns OIDC, Secure/HttpOnly session cookie and explicit CSRF on every mutation including logout/upload. API 401/403 are structured, not HTML login redirects.
- `/api` and `/auth` errors never fall back to SPA index. Direct intended UI-route refresh works.
- Clear authorized queries/selection/forms and cancel in-flight requests across signout/identity/tenant/role changes. Browser UI checks are not authorization.
- Authenticated metadata/download responses are no-store. Public content-hashed assets may be cached under reviewed policy.
- Verify shared Data Protection keys/certificate loading, callback/correlation and session continuity after App Service restart; local test identities are not live Entra proof.

## G7 after renderer removal

G7 remains required for package/build compatibility, searchable authorized list/details and complete role workflows, CSRF/cookie/OIDC/routes/cache isolation, responsive accessibility and actual same-artifact Azure behavior.

Retired checks: renderer/model/import/context loss, orbit/zoom/reset, list/scene parity, GPU/FPS and scene-transfer budgets. T31 is retired; T12/T32 do not depend on it. Retiring a UI subcheck does not mark any native/intake/security/cloud gate passed.

Current design targets: initial app JS≤300 KiB compressed, readable HTML controls while data loads, responsive 390 px and 200% zoom/reflow, keyboard/file picker/dialog focus, reduced motion, clear unknown/error/expired states. Measure actual transferred bytes/load timing on named browser/device/network profiles before performance claims. No FPS target remains.

Existing G0–G6 requirements are unchanged: tenant/license/consent, service-owned per-copy rights and independent inspection, Office A/B own edit/save and crossed-copy denial, scanner/hash/OOXML/intake/publication gates, SQL/Blob durability/recovery and Azure runtime/RBAC/private networking.

Full G7 remains NOT PASSED until current product/live/device/cloud evidence exists. Preserve historical records rather than rewriting their old results as list-only success. See current evidence and docs/07 for the next dependency: T06 durable SQL, then intake/worker/full workflows.
