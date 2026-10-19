# Folder navigation preview — 19 October 2026

User-authorized UI experiment: folder tree + file list + HTML details, now using adapted source from the supplied [Magic UI File Tree](https://magicui.design/docs/components/file-tree). This extends the HTML workspace under ADR-012; it does not restore 3D or implement a SharePoint/OneDrive connector.

## Implemented behavior

- Desktop: left folder navigation, middle authorized file list, right detail/actions. Medium layouts stack details under the list; narrow layouts stack panels and allow native Browse folders disclosure collapse.
- Expanding/collapsing folders and selecting a folder are separate operations. Current folder includes its descendants; All documents returns to the supplied authorized page. Breadcrumbs navigate ancestors.
- Search intentionally spans all supplied authorized folders so users do not need to remember a path. The selected folder scope resumes when search clears. Selecting a folder clears search/previous document selection. Business-owner filtering prunes the directory/counts using only matching authorized documents.
- Tree keyboard: roving tabindex, Up/Down, Left/Right collapse/expand/parent/child, Home/End, Enter/Space selection and type-ahead. Focus is distinct from selection. Nested tree/group semantics and explicit English labels follow the [WAI-ARIA tree pattern](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/). This is not accessibility certification.
- Folder projection uses stable IDs and explicit document-ID membership, not filename/path parsing or inferred business ownership. Unknown document IDs and branches without visible documents are pruned. Duplicate IDs/cycles, more than 200 nodes or depth over 12 fail closed to an empty directory while the authorized file list/search remains available.
- All tree/filter/selection state is in the existing authorized workspace and resets with its principal/role boundary. Folder selection confers no copy/publish permission.

## Source and component decision

Following explicit user approval to adopt the component, vendored the Magic UI [upstream source at commit ec1cce6c4192c0aaac279dd7e53537ccd5c99d44](https://github.com/magicuidesign/magicui/blob/ec1cce6c4192c0aaac279dd7e53537ccd5c99d44/apps/www/registry/magicui/file-tree.tsx) on 19 October 2026. This supersedes the earlier original-only implementation. `Tree` and `Folder` now render through upstream Radix Accordion and Lucide icons. MIT copyright/license is retained in source and shipped at `/licenses/magic-ui.txt`.

Adaptations: controlled selected/expanded state and callbacks connect to the existing workspace; row selection expands while the caret toggles; the existing roving-focus keyboard adapter supplies tree/group semantics. Native local Button/scroll wrappers replace upstream shadcn wrappers; no shadcn CLI installation is required. Semantic colors, Inter, 44px rows, focus styles and reduced-motion rules follow this application. No decorative animation was added. Unused upstream File/CollapseButton APIs are retained for provenance but are not used by this workspace.

Pinned `@radix-ui/react-accordion` 1.3.1 and `lucide-react` 1.59.0 after checking published React 19 peer support. Build/browser verification establishes local compatibility only. The source license and dependency inventory are recorded in component-register.md; accessibility certification is not claimed.

## Data boundaries and future work

Only the separate development entry `spike.html` supplies a nested synthetic Operations/Planning/Resources/Finance hierarchy. It displays SharePoint and OneDrive for Business as **Not connected** text, without a connect action, external IDs or API calls. The production entry contains no fixture paths or future-source placeholder panel; without a folder API it shows only a generic Gateway library grouping of its supplied authorized document records.

`FolderNode` is a UI projection, not a new ExportJob.SourceKind, source-storage contract or tenant catalog. There is no persisted folder CRUD, Graph token, connector, app consent, remote browsing, raw download or source import. Request-copy body and all existing server release/intake/security gates are unchanged. Simulation still offers no copy/download.

A future connector requires a separate scoped design/authorization: server-side permitted folder metadata (including safe ancestor names), bounded lazy loading/paging, source-version binding, freshness/deletion/move behavior, current caller authorization and existing intake/publish eligibility. App-level Graph visibility cannot be treated as end-user access. Do not serialize an entire service-visible tenant tree to the browser. Retain the approved synthetic unprotected-XLSX scope; protected-source decryption remains outside scope.

## Evidence

Current build/browser checks and screenshots are recorded in `docs/evidence/2026-10-19-folder-tree.md` after verification. Existing native/scanner/Office/SQL/Azure gate statuses do not improve simply because the UI preview works.
