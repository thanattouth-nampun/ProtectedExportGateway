# 07 — Implementation backlog and delivery gates v4.2

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

ลำดับนี้คือ dependency ไม่ใช่คำสั่งเปิดหลาย agents ไม่มีการกำหนด timeline เป็นสัญญา ให้ estimate หลัง G1 เพื่อทราบ environment complexity

ADR-013 execution update: first finish the standalone scope/guarded owner-only source spike, then run the approved protected-source G1/G2 experiment before expanding Library screens. Preserve the completed T06 SQL checkpoint. T07/T22 must bind portal ownership to verified original RMS ownership; T23–T26 must redesign protected content scan/structural evidence. HR report paths remain. External connectors are deferred. The old unprotected-only sequence below is superseded for the new Library path; no legacy test is new protected-source acceptance.

## Tasks

| ID | Work | Depends | Done when |
|---|---|---|---|
| T00 | Inspect repo/runtime, create solution/docs links, record evidence status | — | read baseline, runtime inventory และ open issues ชัด; ไม่ overwrite existing work |
| T01 | Environment worksheet + G0 | T00 | owner/tenant/apps/labels/roles/license unknowns มีสถานะ/ผู้รับผิดชอบ |
| T02 | Headless MIP tool + G1 | T01 for live | reports + UDP A/B apply/inspect/recipient-rights/ownership/freshness evidenced; fake ไม่ผ่าน gate |
| T03 | Manual client checks + G2 | T02 | A/B edit/save own copy and cross-open denial, saved copy denial, report controls; แยก client credentials/caches |
| T04 | Domain report/document source contracts, snapshots + state machine | T00 | release invariants, sourceKind, rights/recipient evidence และ invalid transitions enforced |
| T05 | Synthetic fixtures + XLSX generator | T04 | summary ไม่มี payroll detail, detail bounded, no macros/links และ stable dataset version |
| T06 | EF SQL Server schema, queue, idempotency, lease/fencing | T04 | SQL-compatible DB tests; source/recipient snapshots and cross-user key isolation; restart/stale writer tests |
| T07 | Entra sign-in + roles + report/library endpoints | T01 for live,T04,T15 | tenant/role/document ACL, anti-forgery, requester-scoped jobs และ metadata-only owners ผ่าน tests |
| T08 | Fake adapter + worker pipeline | T05,T06 | error/mismatch/timeout/state tests ผ่าน; simulated jobs ไม่มี download |
| T09 | Real MIP adapter + lifecycle/auth | T02,T08 | exact SDK API semantics documented; real output independently inspected |
| T10 | Private Blob adapter + artifact publication/downloads | T06,T07,T09 | exact version/hash, SQL partial failure, verified pre-stream buffer, requester/recipient controls และ no fallback |
| T11 | Cleanup, supervisor, retry, telemetry | T06,T09,T10 | crash/timeouts/stale attempts/expiry/deletion fault tests ผ่าน |
| T12 | React screens: library/owner/reports/jobs/audit | T07,T08,T15,T22,T27,T30 | UX spec13–15, searchable authorized list/details, owner wizard and clear live/simulated state |
| T13 | G3 integration matrix + repository setup guide | T03,T10,T11,T12,T26 | v3 tests/evidence/reproducible sanitized config ครบ |
| T14 | G4 local showcase + value review | T13,T17,T26 | owner upload→publish→A/B copies + reports/version/withdraw demo runs จริง; limitations ชัด |
| T15 | Document metadata/versions/ACL/recipient contracts + synthetic fixtures | T04 | O1/O2/A/B mappings ชัด; seed uses validation contracts; no arbitrary type/source URL |
| T16 | Document-copy request/domain/worker rights profile | T02 for live,T08,T15 | fresh descriptor/recipient per job, pinned source, no user accumulation; readback mismatch blocks |
| T17 | Per-user integration/access suite | T03,T10,T12,T16 | U01–U12 และ API denial/reuse/source race tests ผ่าน |
| T18 | Azure Bicep/parameters/network/RBAC/cost/management plan | T01,T04 | templates locally validate, no secrets, resource estimate/what-if plan/rollback reviewable; no provisioning assumed |
| T19 | Azure provision/bootstrap/credential/scanner setup | T02,T03,T18 + concrete target/cost authorization; T24 locally where available | approved resources/quarantine/promotion scopes/private access/AV prerequisites verified; if scanner unavailable locally, scoped authorized VM spike precedes live publish |
| T20 | Azure deploy + G5 | T13,T17,T19,T26 | v3 live intake/scanner/owners/SDK paths, RBAC/restart/cleanup/access tests evidenced |
| T21 | Cloud showcase handoff | T14,T20,T26,T29,T32 | G0–G7, list/workflow demo/runbook/cost/teardown evidence; no production claim |
| T22 | Owner lifecycle data/API: Draft metadata/ACL/revisions/publish/withdraw | T04,T06,T07,T15 | Publisher+owner, If-Match, transactional audit/current-pointer semantics; private drafts |
| T23 | Bounded upload/quarantine and durable intake | T06,T07,T22 | file/request limits, measured hash/version, idempotency, interrupted/orphan handling and no raw endpoints |
| T24 | Scanner/helper feasibility (G6 first portion) | T01,T23 | actual engine/verdict/freshness/privileges/timeout/hash semantics proven; exit0 alone rejected |
| T25 | Intake worker: scan→OOXML validation→source promotion | T23,T24 for live | exact source/evidence Eligible, source unique/pinned, unknown/fake never live publish; fair scheduler |
| T26 | Full I01–I16 suite + final G6 | T12,T17,T25 | live owner intake/publish/version/withdraw lifecycle and race/fault cases proven; Azure repeats in T20 |
| T27 | v4.2 UX/IA/stack decision | T00 | docs/13–15 and ADR-012 adopted; searchable HTML workspace, role/state/source decisions |
| T28 | Component/font source/license/compatibility register | T27 | React/Vite compatibility, exact licenses/peer versions/dependencies reviewed before reuse; public assets contain no private data |
| T29 | Responsive/accessibility/UX checks X01–X12 | T12,T28 | keyboard/dialog/reflow/reduced-motion/error states tested; prototype results separated from production/user pilot |
| T30 | React/TS/Vite + .NET same-origin BFF skeleton | T07,T27 | single web artifact, typed API contracts/session/CSRF/401/403/API-vs-SPA routes; real auth proof separated from fixture auth |
| T31 | Retired: former renderer spike | — | Removed by explicit user decision ADR-012; no successor task depends on renderer work |
| T32 | Full G7 frontend/cloud regression | T12,T20,T29,T30 | Search/filter/details, identity-cache/CSRF/restart/routes/error/reflow and measured load budgets |

T04–T08,T15, fake T16/T23/T25, T18 และ T22 ทำได้ระหว่างรอ tenant/scanner/subscription โดยไม่เปลี่ยน live gates เป็นผ่าน T09/T10 live release ต้องผ่าน SDK gates; live publish ต้องมี G6 evidence; T19 ต้องมี target/cost authorization ไม่มี subscription ไม่ควรหยุด independent repo work

## Milestones

### M0 — Feasibility demonstrated

G0/G1/G2 ผ่านทั้ง report/UDP paths มี tool ที่ทำซ้ำได้, versions, limited rights/ownership model, recipient inspection semantics และ client evidence ถ้าไม่ผ่าน ให้ report blocker พร้อม next proving check ก่อนขยายงาน live

### M1 — Workflow scaffold demonstrated

Domain/queue/auth/fixtures/fake pipeline ทำงาน และ negative tests ผ่าน หน้าเว็บต้องแสดง Simulated ไม่มี successful download milestone นี้ไม่ใช่ MIP integration

### M2 — Integrated live MVP

Real SDK, release/download/cleanup/recovery controls ทำงานตาม G3; automatic tests และ manual client tests ไม่ขัดกัน

### M3 — Reviewable showcase

G4/G5/G6/G7 ผ่าน มี React list/detail + upload-to-download Azure demo/runbook/cost/evidence/known limitations. Local/fake results remain separate from live/cloud acceptance.

## Suggested implementation artifacts

- `global.json`, central/exact NuGet versions, package lockfiles
- solution/projects ตาม AGENTS.md โดยไม่มี unused architecture layers
- sanitized config example และ config validation
- EF migrations, worker supervision/run instructions
- synthetic fixtures และ fixtures provenance note
- `tools/MipFeasibility` พร้อม command help และ nonzero failure exit codes
- `tools/SeedDocuments` สำหรับ fixtures/tests ผ่าน validation contract เดียวกัน, owner upload UI, scoped scanner helper และ Azure Bicep/parameter examples ตาม docs/11–12
- test suites แยก unit/integration/live; environment-dependent tests ต้องแสดง skipped อย่างตรงไปตรงมา
- `docs/evidence/<run-id>.md` และ machine-readable manifest ที่ไม่มี secrets
- developer setup/run/test commands ที่ทดสอบจริง ไม่ใส่คำสั่งสมมติเป็น verified
- React list/detail screens, semantic tokens/component provenance, X01–X12 and G7 build/device/session/error evidence per docs/13–15

## Prioritization and stopping rules

เพิ่มฟีเจอร์เฉพาะที่ช่วยพิสูจน์ invariant หรือ demo ห้ามใช้เวลาทำ dashboard ก่อนเข้าใจ SDK auth/ownership ถ้า app-only ใช้ไม่ได้ ห้ามแก้ requirement ให้เหลือ mock แล้วบอกสำเร็จ ให้บันทึก ADR และเสนอ flow ที่ทดสอบได้พร้อม tradeoff

ไม่ให้ AI classification, PDF, approval workflow หรือ Data Map เป็น prerequisite ของ MVP ถ้ามีเวลาเพิ่มหลัง G4 ค่อยเสนอ phase 2 ตาม feedback และขอบเขตใหม่

## Handoff report template

The earlier local checkpoint is historical evidence in evidence/2026-10-19-local-checkpoint.md. ADR-012 removes T31/renderer acceptance from current scope. At that checkpoint T06 durable SQL, T08 worker, T22/T23 intake APIs, full product screens and all live gates remained required. Subsequent checkpoint updates follow below.

Subsequent T12 UI experiment: user approved folder-tree/list/details with synthetic preview and keyboard/global-search/breadcrumb checks. See folder-navigation-preview.md and evidence/2026-10-19-folder-tree.md. This does not complete T12 or authorize a SharePoint/OneDrive connector; T06 and all live gates remain required.

Subsequent T06 checkpoint: SQL Server EF schema/queue, immutable copy/report submissions, requester isolation, leases/fencing, retries/recovery and transactional audit are implemented and tested on isolated local SQL Server databases. See sql-queue-checkpoint.md and evidence/2026-10-19-sql-queue.md. This is a storage/queue milestone; T07/T08/T22/T23/T10 and live gates remain required before workflow acceptance. There is no Ready writer or downloadable artifact.

ADR-013 preparation checkpoint: standalone scope/precedence updates, removed connector placeholders, pure observed-source/copy rules and guarded owner-only preflight/live-candidate commands are implemented. 107 local tests plus 5 offline CLI checks pass; no live owner-only authentication/inspection or G1/G2/G6 acceptance. See evidence/2026-10-19-owner-only-preparation.md and owner-only-spike-runbook.md. Complete the target/permission worksheet and obtain scoped approval before the live experiment.

```text
Completed tasks:
Changed files:
Commands actually run and results:
Mode: LiveMip / Simulated
Tenant/cloud/intake/frontend gates: G0..G7 status and evidence references
Security/control failures still open:
Decisions changed and reasons:
Next task and blocking input (if any):
```
