# 04 — Environment prerequisites and feasibility gates v4.2

> ADR-014 access update (20 October 2026): [Self-service Member workspaces](member-self-service-workspace.md) supersedes Publisher-only/pre-enrolled-owner empty-draft access below. Every verified internal tenant Member gets own private drafts; source RMS-owner mapping, reviewed recipients, scan/release and report/auditor controls remain. Approved tenant claim/enrollment rollout and live acceptance are tracked separately in ADR-014.

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

สถานะเริ่มต้นทุก gate = NOT RUN. Checklist คือสิ่งที่ต้องตรวจ ไม่ใช่หลักฐานว่า environment มีอยู่แล้ว

## รายการส่งให้ทีม environment

| ต้องเตรียม/ยืนยัน | ผลลัพธ์ที่ต้องได้ | ส่ง secrets อย่างไร |
|---|---|---|
| Test tenant และ license | Tenant ID; ผู้รับผิดชอบยืนยัน entitlement ตาม scenario และผู้ใช้ | ส่งเฉพาะ metadata ที่จำเป็น |
| Rights Management readiness | Service พร้อมใช้งานใน tenant และไม่มีข้อจำกัดที่ขัดกับ demo | บันทึกสถานะ/วันที่ตรวจ |
| Labels | Internal label-only และ Confidential encrypted label, ใช้กับ Files; label IDs, scope, recipients และ policy publication ที่ตรง flow | เก็บ config จริงนอก git |
| UDP label/profile | label สำหรับ custom recipient permissions, candidate VIEW/DOCEDIT/EDIT; ไม่ใช้ broad template recipients | G1 inspect actual recipients/rights |
| Library fixtures/publishers | synthetic XLSX สองไฟล์ที่ O1/O2 อัปโหลดผ่าน UI, Publisher roles/ACL A/B/source versions | unprotected sources; seed only tests, no validation bypass |
| Intake/scanner | quarantine scope, native AV availability/mode/version/signature updates, narrow helper, OOXML bounds | G6 ก่อน live publishing; no real employee uploads |
| Recipient mapping | validated tid/oid → RMS address สำหรับ A/B พร้อม mapping version/validation date | ไม่รับค่าจาก request |
| Web registration | Single-tenant OIDC app, redirect URI, app roles และการ assign test users | Cookie/OIDC credentials ใช้ secret store |
| Processor registration | Separate service principal, admin consent สำหรับ candidate permissions, certificate credential | Private key ไม่ส่งผ่านแชต/commit |
| Test users | requester ที่มี PayrollExporter, requester ที่ไม่มี role, recipient ที่ได้รับสิทธิ์ และผู้ใช้ที่ไม่มีสิทธิ์เปิดไฟล์ | ไม่ส่งรหัสผ่าน |
| Host | Supported Windows x64, .NET candidate runtime และ native SDK prerequisites | บันทึก versions/build |
| Client | Supported Excel/Microsoft 365 Apps, signed-in tenant identities และ network access | ใช้ synthetic file เท่านั้น |
| Network | DNS/TLS/proxy/auth/firewall ถึง Entra, policy และ RMS endpoints ตาม SDK ปัจจุบัน | ไม่แก้ TLS validation หรือ bypass security controls แบบครอบจักรวาล |
| Cost/release | ยืนยัน trial/subscription limitations; deployment target และขอบเขต internal/public release | ไม่เปิด billing อัตโนมัติ |
| Azure target | subscription, region, resource group, SKU/quota, ingress audience และ cost ceiling | IaC/what-if/cost review ก่อน provision |
| Cloud dependencies | App Service, Windows VM, Azure SQL, private Blob, Key Vault, managed identities/network/monitoring | ตาม docs/11; ยังไม่มี resource |
| Frontend tooling | supported Node LTS + compatible React/TS/Vite/router/query/forms and browser profiles | G7; no second production Node server |

Confidential recipients ต้องเป็นกลุ่มทดสอบแคบ ไม่ใช้ All Employees แล้วอ้างว่า HR-only summary เป็น label-only จึงไม่มี expectation ว่าจะปฏิเสธผู้เปิดที่ไม่มี RMS rights

## G0 — Business and environment confirmation

- ยืนยัน report สองแบบ, mapping owner, synthetic dataset และ target audience
- ระบุใครอนุญาตสร้าง Entra apps/labels และใครมีสิทธิ์ admin consent
- ยืนยัน license/terms กับผู้รับผิดชอบ tenant โดยอ้าง feature-level documentation ไม่สรุปว่า service account หนึ่ง license ครอบคลุมทุกผู้ใช้
- Reports ใช้ label-only/admin-defined protection; Library ใช้ user-defined permissions label และ per-requester limited edit/save profile ตาม docs/10
- ยืนยัน business owner เป็น metadata และ service principal เป็น candidate protection owner ไม่ delegate requester เป็น owner; บันทึก privileged exceptions
- ยืนยัน Azure deployment parameters/cost owner และ management path; local work ทำได้ก่อน provisioning
- ยืนยัน Owner/Publisher lifecycle, bounded same-tenant ACL requester allowlist, withdrawal/non-revocation และ scanner/helper strategy ตาม docs/12
- ตกลง TTL, data root, test roles และถ้าจะ publish ให้แยก approval/deployment work

Exit: environment worksheet ไม่มี unknown ที่จำเป็นต่อ G1 หรือระบุ blocker ที่มีเจ้าของชัดเจน งาน coding ที่ไม่ขึ้นกับ tenant ทำต่อได้

## G1 — Headless MIP feasibility; MUST pass before live web integration

สร้าง console tool `tools/MipFeasibility` ที่รับ local synthetic XLSX และ config จาก secret/config store

ลำดับ:

1. Restore/build บน Windows x64 ด้วย .NET candidate; บันทึก SDK/package/native versions
2. Auth delegate ใช้ certificate credential กับ resource ที่ SDK ขอ ภายใต้ allowlist ของ trusted authorities/resources; ห้าม log token
3. สร้าง context/profile/engine ด้วย app-only identity ตามเอกสารของ exact SDK; บันทึก identity/policy settings ที่ใช้จริงโดยไม่เก็บ cache/token
4. ดึง labels และหา configured IDs ใน identity context นั้น ถ้า label หาไม่พบอย่าสร้าง ID แทน
5. Apply label-only ให้ input → output ใหม่ → inspect ใหม่ → expected label และ protection=false
6. Apply confidential label ให้ input ใหม่ → output ใหม่ → inspect actual protection=true และ label ID ตรง
7. ระบุว่า SDK properties ที่ใช้หมายถึงอะไร โดยเฉพาะความต่างระหว่าง actual protection กับ flag ว่า protection มาจาก label; flag เดียวไม่พอถ้า semantics ไม่ครอบคลุม
8. ทดสอบค่าที่ผิด, ไม่มี label และ protection failure แล้ว tool ต้อง exit nonzero ไม่มี success artifact ที่เผยแพร่
9. ทดสอบ cold start, warm cache, engine disposal และ policy refresh/freshness strategy; ตรวจว่า auto-assignment จาก business rule ใช้ AssignmentMethod ใดโดยไม่ bypass policy
10. บันทึก sanitized evidence ลง repo เฉพาะ metadata; actual files/caches แยกใน private runtime directory
11. เพิ่ม UDP test: source เดียวกันออก copy A/copy B ด้วย fresh descriptors; label เดียวกัน แต่ independently observed recipients/rights ต้องต่างกันตรง mapping ไม่มีสิทธิ์ข้ามคน
12. ตรวจ exact VIEW/DOCEDIT/EDIT mappings, save behavior และไม่มี unintended EXPORT/OWNER/EXTRACT; หาก API inspect ได้ไม่ครบให้ status unresolved ไม่เก็บเพียง intended descriptor เป็น observed proof

Exit: app-only ทำ report และ UDP apply/independent inspect ได้, runtime/ownership/recipient mapping ชัด และไม่มี unknown ที่ทำให้ Ready invariant ตรวจไม่ได้

ถ้าอ่าน encrypted output ไม่ได้เพราะ privilege อย่าเพิ่ม tenant-wide decryption เพื่อให้ผ่าน ให้ตรวจ API/flow ที่ไม่ต้องอ่าน plaintext หรือเปิด ADR เพื่อเปลี่ยน identity model หากยังพิสูจน์ไม่ได้ gate ยังไม่ผ่าน

## G2 — Client behavior

- เปิด encrypted output ด้วย allowed user และบันทึกว่าอ่านเนื้อหาได้จริง
- เปิดไฟล์เดียวกัน (hash เดียวกัน) ด้วย denied user ใน environment/profile ที่แยก credentials/cache แล้ว
- บันทึก error/status/screenshot โดยไม่เผยข้อมูลลับ; service outage หรือ sign-in failure ที่ไม่เกี่ยวกับ rights ไม่ใช่หลักฐาน denial ตาม label
- ใช้ allowed control ที่เปิดได้และตรวจ configured rights คู่กับ denied result
- เปิด label-only output และแสดง label ได้ตาม supported client โดยไม่อ้างว่า label-only ห้ามอ่าน
- Library: A เปิด/แก้ไข/บันทึก copy A และ B ทำเช่นกันกับ copy B; นำทั้ง original output และ saved protected copies ไป cross-open แล้วต้อง rights-based deny อีกคน
- ตัดสิทธิ์ requester เป็น RMS owner หรือ broad tenant/domain grant ออกจาก test setup; service protection owner privileged behavior บันทึกแยกจาก A/B

Exit: allowed/denied behavior สอดคล้อง configuration; บันทึก client version, file hash, identities แบบนามแฝง, UTC time และข้อจำกัด

## G3 — Web path and controls

G1/G2 ผ่านแล้วจึงต่อ real adapter เข้ากับ web pipeline ตาม docs/02 ผ่าน authorization, idempotency, verification mismatch, simulated no-download, audit failure, expiry และ reboot recovery ใน docs/08

Exit: มีทั้ง happy path และ failure evidence; Ready ไม่เกิดจาก mock

## G4 — Showcase acceptance

สาธิตสอง reports, O1/O2 owner upload→scan/validate→publish, per-user A/B copies/crossed-copy denial, new version/withdraw และหนึ่ง live failure โดยไม่มี plaintext fallback วัดเวลาจริงและบันทึก feedback โดยไม่อ้าง SLA; G6 ต้องผ่านสำหรับ upload path จริง

## G5 — Azure deployed acceptance

ทำตาม docs/11 บน resource ที่ได้รับอนุมัติ target/cost แล้ว ตรวจ App Service OIDC, native MIP บน VM, SQL durable jobs, MI scoped permissions, private DNS/endpoints, exact Blob version binding, Key Vault credential access, restart/recovery/cleanup และ metrics/log redaction พร้อม runbook/cost/teardown

IaC build/what-if ไม่ใช่ deployment success; local SDK success ไม่ใช่ VM success Status เริ่มต้น NOT RUN

## G6 — Live intake and scanner feasibility/acceptance

ตรวจ candidate AV บน actual Windows host, platform/engine/signature freshness (application target ≤24 ชั่วโมง), callable permissions และ narrow helper isolation โดยไม่ elevate web/MIP worker ใช้ benign synthetic workbook และ controlled vendor-approved non-malicious test procedure ที่ environment owner อนุญาตเพื่อพิสูจน์ detection/rejection handling; fake result ไม่ใช่ AV evidence

พิสูจน์ clean vs remediated/error/unknown/timeout ให้แยกได้: MpCmdRun exit0 ลำพังไม่พอ hash ต้องไม่เปลี่ยน และ no scanning-disabled/exclusion bypass ตรวจ bounded ZIP/OOXML parsing และ reject protected/active content ผ่าน I01–I16 รวม concurrent version/publish/withdraw/ACL, crash/partial writes และ fail-closed publish

Local live scanner evidence ไม่แทน G5 cloud intake test Status เริ่มต้น NOT RUN ไม่มีการสร้าง/รัน malware หรือเปลี่ยน host protection ในการจัดทำเอกสารนี้

## G7 — React HTML workspace and same-origin acceptance

Build compatible React/TS/Vite/router/query/forms versions; test authorized list/search/owner filter/selection/details, keyboard/reduced-motion/reflow/error states, bundle/load behavior on named browser/device/network profiles. ADR-012 retires renderer, camera, GPU/FPS and context-loss checks.

Prove .NET OIDC/session/cookie/CSRF/401/403, reserved API/auth routes vs SPA, direct UI reload, restart continuity and query/selection/in-flight clearance on identity change. Generated DTOs align with the server. Repeat on the same App Service release; Node remains build tooling.

Full G7 remains NOT PASSED. Local tests do not prove live OIDC, deployment/restart, a full accessibility audit or representative-device performance.

## Evidence template

```yaml
gate: G1
status: NOT_RUN # PASS | FAIL | BLOCKED | NOT_RUN
run_id: null
executed_at_utc: null
operator_alias: null
os_build: null
architecture: x64
dotnet_version: null
mip_package_version: null
native_dependencies: []
processor_identity_kind: service_principal
permission_names: []
hosting_environment: local # local | azure
source_kind: ReportExport # ReportExport | DocumentCopy
rights_profile_version: null
recipient_alias: null
observed_recipient_aliases: []
observed_rights: []
intake_run_id: null
scanner_provider: null
scanner_engine_version: null
security_intelligence_timestamp_utc: null
scan_verdict: unknown
source_validation_profile: null
source_blob_version_alias: null
output_blob_version_alias: null
input_sha256: null
output_sha256: null
expected_label_alias: null
observed_label_alias: null
expected_protection: null
observed_protection: unknown
client_access_test: NOT_RUN
result_summary: null
limitations: []
```

IDs ภายในองค์กรอาจไม่ใช่ secrets แต่ใช้ aliases ใน public evidence เพื่อลด tenant metadata exposure บันทึก mapping จริงเฉพาะ controlled configuration

## Network and troubleshooting

เริ่มตรวจจาก error category และ SDK version แล้วทดสอบ DNS, proxy authentication, TLS trust และ endpoint access จาก worker identity จริง อย่าสรุป network ready จาก TCP 443 อย่างเดียว ใช้ current Microsoft endpoint guidance; ห้ามปิด certificate validation, MFA หรือ security product เพื่อบังคับให้ demo ผ่าน
