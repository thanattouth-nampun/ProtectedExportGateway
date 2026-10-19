# 19 October 2026 — folder/tree/list/details UI preview

User authorization: try the discussed folder navigation layout for the existing gateway and future source connectivity. This is a bounded UI experiment; no connector, tenant configuration or resource deployment was authorized or performed.

## Implementation

- Added original `FolderNavigation.tsx`: collapsible nested folders, explicit ARIA tree/group/selection, roving focus, arrow keys, Home/End, Enter/Space, type-ahead and native Browse folders disclosure.
- Integrated folder scope and breadcrumb navigation with the existing document list/details. File search spans all supplied authorized folders; clearing search resumes folder scope. Owner filtering prunes folder counts/branches. All documents clears folder and file selection. Layout supports three panels on wide screens and stacked/collapsible navigation at 390px.
- Added a bounded directory projection from explicit IDs and authorized supplied document membership. Unknown file IDs and empty unauthorized branches are excluded; duplicate IDs, cycles and directory limits fall back to no directory while full authorized list/search remains available. This client projection is not a replacement for server authorization.
- Synthetic Operations/Planning/Resources/Finance hierarchy exists only in the separate dev entry. Future SharePoint/OneDrive labels explicitly say Not connected and have no connection action. Product entry derives only a generic Gateway library grouping from supplied API records; it does not render the fixture hierarchy/future-provider panel. Generic preview copy remains in shared component code, without any external connection data.
- Preserved existing copy request DTOs and simulation no-download behavior. No server/schema/MIP/scanner/rights/intake/ACL authorization behavior changed.
- No package added or installer executed. Magic UI upstream source/license reviewed as reference; implementation/SVG/CSS are original. See [source decision and limitations](../folder-navigation-preview.md).

## Verification and limits

`./scripts/verify.ps1` exited **0**. All **73 tests passed**: 41 unit, 18 HTTP integration, 7 frontend unit, 7 Chromium browser. Locked build, DTO drift/native-load checks and single .NET/React web package passed. No new package was added. Main JS308.81kB/gzip97.03kB and CSS15.40kB/gzip4.12kB are build sizes, not measured load-time results.

The [JSON manifest](2026-10-19-folder-tree.json) records package hashes and gate status. Frontend checks cover supplied-record directory pruning, unknown paths, duplicate/cyclic/oversized folder metadata, ancestor breadcrumbs and full-page search preservation. Browser checks cover actual pointer rows, keyboard tree behavior, folder scope, global search, owner filter, principal-change clearance, no WebGL, narrow-screen/reduced-motion behavior and production sign-in isolation.

[Desktop preview](2026-10-19-folder-tree-desktop.png) · [390px collapsed navigation](2026-10-19-folder-tree-mobile.png).

One test initially clicked the bounding box of an expanded parent treeitem, which includes its child group. The pointer test was corrected to target the visible folder row. The tree's keyboard selection uses the treeitem itself and is tested separately.

These are synthetic/in-process/local browser results, not real SharePoint folders, OneDrive access, live Entra authentication or file protection/denial. No external paging/lazy-loading connector, persisted folder mutations, protected-source ingestion/decryption, live scan, Office cross-copy test, SQL durability or Azure deployment was performed. No full screen-reader/accessibility certification or measured representative-device network/load benchmark.

G0/G1/G6 live blockers, G2–G5 not-run status and incomplete full G7 remain. T06 durable SQL and full intake/worker/product flows remain next; a connector requires a separate scoped design/authorization.
