# 19 October 2026 — HTML list workspace / ADR-012

The user explicitly removed 3D because it does not fit this project. ADR-012 supersedes the former renderer requirement; React/TypeScript/Vite, .NET BFF/Windows MIP worker and all functional/security/native/intake/Azure controls remain.

## Changes

- Removed the renderer module, lazy scene import/error boundary, mesh/camera/selection animation, 3D/List toggle, renderer CSS and stored-view preference handling.
- Removed Three, React Three Fiber, Drei and Three types; npm removed 60 direct/transitive packages, leaving 105 audited packages. Exact retained versions and lockfile remain.
- Document library now uses one HTML list with search, business-owner filter, selected details and existing copy action/denial behavior. English UI, theme, self-hosted Inter and supplied fixture metadata remain.
- Development-only fixture preview remains `spike.html` and is excluded from the release. It does not authenticate, scan, protect, publish or download files.
- Browser regression now targets search/filter/details, identity-cache clearance, keyboard/reduced-motion/reflow and sign-in. A negative GPU-context test confirms the workspace never requests WebGL. No software-GPU flags are used.
- Updated AGENTS/README/START_HERE and docs 01–15, setup/component/environment records. T31 is retired and removed from T12/T32 dependencies. G7 retains frontend/session/accessibility/load/cloud regression; renderer/FPS/context-loss checks are retired.
- The prior assessment is preserved in docs/history; old evidence/screenshots/manifests are historical observations, not current implementation claims.
- Verification cleans the checked workspace-local publish output before packaging and guards against stale renderer chunks/dependencies in the resulting release.

## Verification

`./scripts/verify.ps1` completed with exit code **0**. All **67 automated tests passed**: 41 unit, 18 HTTP, 3 frontend unit and 5 Chromium browser checks. Locked restore, generated DTO drift check, native load and the single .NET/React release build passed. Publish output was rebuilt without the retired renderer chunk or fixture entry; the dependency guard passed.

Package hashes and gate statuses are in the [machine-readable manifest](2026-10-19-list-workspace.json). Build: main JS 303.52 kB, gzip 95.35 kB; CSS 11.91 kB, gzip 3.44 kB. The former 930.97 kB renderer chunk is absent. These are build sizes, not measured network/load-time improvements.

[Desktop list/details](2026-10-19-list-desktop.png) · [390px layout](2026-10-19-list-mobile.png).

The current browser matrix covers 1440×1000 and 390×844, reduced-motion emulation, keyboard selection, synthetic server identity changes and no-WebGL behavior. It does not certify accessibility or real Entra/Office/Azure behavior.

## Gates and next work

The renderer requirement is removed by scope decision, not passed by a test. G0/G1/G6 remain blocked on actual tenant/scanner configuration/evidence; G2–G5 remain not run; full G7 remains incomplete for full product/live/session-restart/device/accessibility/cloud acceptance.

T06 durable SQL/transactions/idempotency/lease/fencing/recovery remains next, followed by intake/worker/full Owner/Reports/Jobs/Audit workflows. No Azure resource, tenant configuration, admin consent, security exclusion or deployment was changed.
