# 19 October 2026 — local feasibility implementation checkpoint

> Historical checkpoint. ADR-012 subsequently removed 3D from the project. Results below describe the earlier code and remain historical evidence; see [current list-only evidence](2026-10-19-list-workspace.md).

Scope: current repository only; no earlier conversation or memory used. This record distinguishes code and controlled tests from live tenant/scanner/client/cloud evidence. Final command outcome is recorded in the adjacent JSON manifest.

Final result: **`./scripts/verify.ps1` exited 0**. All **69 automated tests passed** (41 unit,18 HTTP,3 frontend unit,7 Chromium). Locked restore, DTO drift check, native-load check and automatic client-build/.NET publish passed. [Machine-readable manifest](2026-10-19-local-checkpoint.json) binds local package hashes and gate statuses. No live gate is inferred from this result.

## Delivered implementation

- Solution/global SDK pin, explicit NuGet source, exact NuGet/npm versions and lockfiles, runtime/secret ignores.
- `tools/MipFeasibility`: installed API signature inventory, native loader probe, certificate-backed headless initial apply/inspect candidate for label-only/payroll/A/B, fresh descriptors/handlers, allowlisted challenges, expected owner comparison, private output and sanitized observations. It cannot declare full G1 PASS.
- Domain/application foundation: principal/source/recipient snapshots and fingerprints, release/download predicates, state transitions, owner+Publisher/revision/ACL/current-publication rules; trusted Clean/hash/freshness predicate; scan-before-parse orchestration; restricted bounded OOXML parser. Persistence and scanner/process adapters are not implemented.
- ClosedXML synthetic headcount/payroll generator (10 synthetic payroll rows), create-only private fixture generator; no seeding shortcut into Eligible/Published.
- ASP.NET Core BFF skeleton: disabled-until-configured Entra OIDC, code/PKCE, server cookie, tid/oid/issuer validation, no saved tokens, CSRF, structured cookie 401, session/logout routes, no-store, reserved API/auth routes, known SPA routes and development OpenAPI. LiveMip startup is blocked. Actual stores/readiness deliberately return unavailable. No durable workflow endpoint success is fabricated.
- React/TS/Vite/Tailwind/Router/Query foundation. Real lazy R3F WebGL scene with authorized projection, Owner groups, mesh selection→HTML details, controlled orbit/zoom/reset, restrained selection motion, demand frames/hidden-tab unmount, capped DPR, reusable geometry/materials with disposal, searchable List mode. Query cancellation/clear and workspace remount on identity/role change.
- Separate development-only `spike.html` with three synthetic metadata fixtures; no sign-in, persona selector, scan, mutation or download. Production entry contains no fixture metadata. Application-owned UI is English, approved forest theme and official Inter 4.1 WOFF2 (400/500/600), self-hosted with license and system fallback.
- xUnit/HTTP/Vitest/Playwright tests, reproducible `scripts/verify.ps1`, prepared SHA-pinned test-only Windows GitHub Actions workflow. Hosted CI has not run.

## Actual local checks

| Command/check | Actual result and limitation |
|---|---|
| Read README/AGENTS/START_HERE/docs01–15/prototype README | Completed; ADR-011/addenda adopted. Historical “no code” statements are baseline date context |
| `dotnet --info`, Node/npm/tool inventory | SDK 10.0.300, runtime/ASP.NET 10.0.8, Node24.16.0/npm11.13.0; SDK inventory raised SCM access denial. No admin elevation used for AV/application |
| Exact package metadata, restore, `npm ls --depth=0` | Compatible resolved peer graph; TypeScript7 rejected in favor of5.9.3 required by generator; no peer-check bypass |
| `dotnet run --project tools/MipFeasibility -- api` | Managed signatures read from installed SDK1.18.124; not live API success |
| `native-check` | PASS, assembly1.18.124.0, native File SDK initialization on Windows x64/.NET10. No auth/profile/policy call |
| `run .runtime/missing-config.json` | Nonzero exit2, safe FileNotFoundException category; no output artifact/tenant action |
| `dotnet test tests/Gateway.UnitTests --no-restore` | **41 passed**: release mismatch/unknowns/owner/recipient separation, mapping/fingerprint/expiry/state, scan non-clean/simulated/hash rejection, owner/revision/draft/publish/withdraw, generated summary/detail and adversarial ZIP/XML cases |
| `dotnet test tests/Gateway.IntegrationTests --no-restore` | **18 passed**: actual cookie401/no redirect, test-identity HTTP CSRF/cookie binding/no-store/simulation denial, API/auth404 vs SPA, compiled UI route reload, OpenAPI export. Test auth exists only in test assembly; not Entra proof |
| `npm test` | **3 passed**: shared authorized projection/filter/owner grouping/bounds |
| `npm run test:browser` | **7 passed**, Chromium156.0.8078.4/Playwright1.64.1, configured SwiftShader software WebGL: actual Canvas and projected mesh pointer selection, list/search/camera controls, identity-cache/selection clearance, real WEBGL_lose_context extension, aborted dynamic import, denied renderer initialization, keyboard/reduced-motion390px no page overflow and production sign-in/no fixtures |
| OpenAPI export + `npm run contracts` | Generated from actual BFF; byte-hash drift check in verify script. No handwritten DTO surrogate |
| `npm run build` | TypeScript and Vite production build; deferred scene chunk; no development entry emitted |
| `dotnet list ... package --vulnerable --include-transitive` / npm install audit | No known vulnerabilities reported by current feeds after pinning patched Microsoft.OpenApi2.7.5. Not a security certification |
| `dotnet publish src/Gateway.Web -c Release -o .runtime/publish` | PASS: one .NET web release contains built React assets, fonts/license; no spike.html. Not Azure deployment |

One initial parser test exposed ClosedXML's standard package-root relationship targets and generated core-properties `.psmdcp` path. The restricted allowlist was corrected and both generators pass. Traversal/external/duplicate/active-content/Dtd/high-ratio cases remain rejected. Every parser test uses local synthetic bytes. A constructed test Clean record is not a trusted AV observation. No uploaded file has been scanned/promoted/published.

## Build/device evidence limits

Vite production report: initial app JS305.66kB / gzip96.03kB; lazy scene930.97kB / gzip247.37kB; CSS about13.16kB / gzip3.70kB. Fonts111,268/114,348/114,812 bytes. These are build estimates, **not measured network transfer or FPS**. The >500kB scene-chunk advisory remains visible; scene is deferred from core UI. R3F/Three emits a Clock deprecation warning; injected renderer/import failures intentionally emit console errors and recover to List.

Browser profiles: desktop1440×1000, narrow390×844, reduced-motion emulation; browser/software renderer rather than an agreed integrated-GPU office laptop. No representative device FPS, slow-network load budget, low-performance adaptive threshold, offscreen IntersectionObserver policy, full200% zoom/screen-reader audit, mobile hardware test or usability pilot was performed. No accessibility/production-readiness certification.

Screenshots are synthetic harness observations, not production integration or protection proof. HTML labels/actions remain outside Canvas; screen-reader/manual X01–X12 acceptance remains open.

[Desktop scene](2026-10-19-scene-desktop.png) · [390px list/detail](2026-10-19-scene-mobile.png).

## Gate/milestone status

| Gate | Status | Blocking proof |
|---|---|---|
| G0 | BLOCKED for live | Tenant app/labels/license/roles/mapping/ownership/config confirmation; user confirms tenant/subscription exist, not their exact setup |
| G1 | BLOCKED; native-load subcheck passed | App-only auth/label apply and fresh-output rights/recipient/owner observations, negative/freshness/cold-warm tests |
| G2 | NOT RUN | Isolated Office A/B own edit/save/reopen and crossed original/saved-copy denial |
| G3 | NOT RUN | SQL queue/transaction/fencing/recovery, real worker/artifact/Blob/download integration and full negative suite |
| G4 | NOT RUN | Full Owner upload→publish→copy/report/withdraw showcase |
| G5 | NOT RUN | Approved concrete target/cost and actual Azure deployment/RBAC/private-network/restart tests |
| G6 | BLOCKED | Actual AV/helper/privileges/engine/signatures/reliable verdict/hash and complete I01–I16/process evidence |
| G7 | PARTIAL LOCAL CHECKS; full gate NOT RUN | Live OIDC/session restart, full product/list workflows, representative performance/accessibility and same-artifact Azure regression |

This is a **verified local foundation/spike checkpoint**. It is not M0, M1, M2 or M3 completion. T00/T01 inventory/worksheet are complete with listed live blockers; T02/T04/T05/T15/T28/T30/T31 have only the implemented/tested scope above. T06 SQL, T08 worker, T22/T23 intake APIs, T12 complete Owner/Reports/Jobs/Audit screens and all live paths remain required. No Azure/Entra mutation, grant, resource/cost activation, AV command/exclusion or deployment occurred.

## Next dependency

Implement T06 with a SQL Server-compatible local test instance: EF schema/migrations, atomic audit/admission/idempotency, leases/fencing/restart/orphan tests. Then wire T07/T22/T23 and simulated T08 with terminal Simulated/no artifact, followed by full role workflows. Complete G0/G1/G2 with operator-owned private configuration in parallel; no secrets through chat. [Environment worksheet](../environment-worksheet.md), [setup](../developer-setup.md), [deployment boundary](../deployment-runbook.md) and [component/license register](../component-register.md) provide handoff details.
