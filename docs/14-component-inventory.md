# 14 — Component sourcing and React implementation v4.2

> ADR-014 access update (20 October 2026): [Self-service Member workspaces](member-self-service-workspace.md) supersedes Publisher-only/pre-enrolled-owner empty-draft access below. Every verified internal tenant Member gets own private drafts; source RMS-owner mapping, reviewed recipients, scan/release and report/auditor controls remain. Approved tenant claim/enrollment rollout and live acceptance are tracked separately in ADR-014.

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

ADR-012 (19 October 2026) removes 3D. Use one React/TypeScript HTML workspace with the .NET BFF. Preserve the approved semantic theme and self-hosted Inter. No catalog source has blanket approval.

21st.dev is a component discovery catalog. Review original source, exact author/repository/file/revision, license/notices, dependencies, accessibility, bundle cost and modifications before copying. Reject unknown licensing; implement an original semantic alternative. Do not run unreviewed catalog installers or remote runtime scripts. Next/server-only components do not automatically work in Vite.

| Component | Current decision | Required behavior |
|---|---|---|
| Navigation/list/detail/alerts | Original repository components | Role-aware navigation, authorized search/owner filtering, readable details, responsive reflow |
| Folder navigation | Vendored/adapted Magic UI Tree/Folder; pinned source, license and dependencies in component-register.md | Controlled selection, authorized projection, breadcrumb/global search, tree keyboard and mobile disclosure; Radix Accordion/Lucide used, no connector |
| File picker/dropzone | docs/09 S31 visual reference only; exact source review before reuse | One bounded XLSX; native keyboard picker, errors and measured upload progress |
| Intake/job progress | Original state labels first; staged-progress catalog remains reference | Actual server stages, no invented percent/ETA; reduced motion |
| Forms | React Hook Form/Zod selected; server validation authoritative | Retained values, inline and summary errors; no arbitrary owner/label/rights/recipient overrides |
| Confirmation dialog | Reviewed shadcn/21st source or original semantic implementation | Title/description, focus trap/Escape/return, inert background; exact publish/withdraw effects |
| Table/cards | Original list now; review any future table source individually | Authorized paging/sort/search, current version/owner, narrow-screen task parity |
| Renderer/model/texture | Removed by ADR-012 | Do not reinstall Three/R3F/Drei or add replacement decorative renderer |

Shared TanStack Query state is requester-scoped. Clear selection/forms/caches and cancel in-flight requests at identity/tenant/role boundaries. Server-generated DTOs, HttpOnly cookie/CSRF, no persistent browser tokens and one .NET/static web release remain required. Node is build tooling only.

A component's visual polish is not authorization, MIP, scan or accessibility evidence. Keep English UI and supplied metadata/tenant labels intact. See [component register](component-register.md), [UX specification](13-ux-ui-design.md) and [stack assessment](15-long-term-stack-assessment.md).
