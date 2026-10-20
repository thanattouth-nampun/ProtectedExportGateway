# 01 — Product requirements v4.2

> ADR-014 access update (20 October 2026): [Self-service Member workspaces](member-self-service-workspace.md) supersedes Publisher-only/pre-enrolled-owner empty-draft access below. Every verified internal tenant Member gets own private drafts; source RMS-owner mapping, reviewed recipients, scan/release and report/auditor controls remain. Approved tenant claim/enrollment rollout and live acceptance are tracked separately in ADR-014.

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

## Problem and value hypothesis

รายงาน XLSX ที่ออกจากระบบธุรกิจอาจไม่มี classification/protection ที่องค์กรคาดหวัง ถ้าผู้ใช้ต้องเลือก label ภายหลัง ความสม่ำเสมอของการติด label ขึ้นกับการทำงานแต่ละครั้ง

สมมติฐานของโปรเจ็กต์: การกำหนดกฎที่จุด export จะลดไฟล์ที่หลุดออกไปโดยไม่มี label ที่กำหนด และให้หลักฐานว่าระบบสร้าง/ตรวจ/ปล่อยไฟล์ใดแก่ใคร ต้องยืนยันกับระบบจริงและผู้ใช้งาน ยังไม่มีข้อมูลเชิงสถิติหรือ customer interview ในชุดเอกสารนี้

เพิ่ม Document Library ในเว็บเดียวกัน: เอกสารมี business owner ต่างกัน และ A/B ที่ได้รับอนุญาตขอเอกสารเดียวกันจะได้สำเนาที่ป้องกันเฉพาะตน A เปิดสำเนา B ไม่ได้และกลับกัน Hosting บน Azure เป็นส่วนหนึ่งของ deliverable ตาม docs/11

MVP ทำให้กระบวนการนี้ทดสอบซ้ำได้ ไม่ใช่ผลิตภัณฑ์ DLP หรือการรับรอง compliance

## Personas and roles

| Role | สิทธิ์ในเว็บ |
|---|---|
| ReportReader | ดู summary fixture และ export headcount-summary ของตน |
| PayrollExporter | มีสิทธิ์เพิ่มสำหรับ export payroll-detail และดูข้อมูลจำลองที่เกี่ยวข้อง |
| LibraryReader | ดูและขอ protected copy เฉพาะ document ที่ผ่าน ACL; แค่มี role ไม่ให้เข้าถึงทุกเอกสาร |
| DocumentPublisher | สร้างเอกสาร/อัปโหลด; mutation ของเอกสารเดิมต้องมี role นี้และเป็น Owner |
| Document business owner | ผู้อัปโหลด/ผู้สร้างจาก Entra tid/oid; จัดการ metadata/ACL/version/publish/withdraw ของตนเมื่อมี Publisher role; ไม่ได้ RMS rights ของทุก copy อัตโนมัติ |
| ExportAuditor | ดู metadata/evidence ของทุก job ใน tenant; ไม่มีสิทธิ์ดาวน์โหลดรายงานของคนอื่น |
| Environment operator | เตรียม apps/labels/certs/runtime และ config ผ่าน deployment process; ไม่ใช่หน้าแก้ policy ใน MVP |

ใช้ Entra app roles หรือ mapping authorization ที่ validate แล้ว ไม่ตัดสิน role จากข้อความที่ browser ส่งมา ผู้ที่ได้รับสิทธิ์อ่าน protected content เป็นอีกชุดสิทธิ์หนึ่ง แยกจาก role ของเว็บ

## MVP scope

- Single tenant, Azure App Service web + Windows VM worker, Azure SQL/Blob/Key Vault/Monitor ตาม docs/11; single worker concurrency = 1
- Reports ใช้ synthetic dataset/version คงที่; Library ให้ Publisher อัปโหลด synthetic unprotected XLSX ผ่าน bounded intake ไม่ใช้ข้อมูลจริงใน demo
- XLSX เท่านั้น ไม่ใช้ macro, external links หรือ embedded objects; demo owners O1/O2 อัปโหลดผ่าน UI; seed tool ใช้ fixtures และไม่ bypass validation
- Intake: private quarantine → malware scan → bounded OOXML/unprotected-content validation → Eligible version → Owner publish; รายละเอียดตาม docs/12
- สอง report definitions: `headcount-summary` (aggregated; ไม่ใส่ชื่อ/เงินเดือนรายบุคคล) และ `payroll-detail` (ข้อมูลพนักงานสังเคราะห์)
- สอง business mappings: summary → configured internal label (expected encryption = false), payroll → configured confidential label (expected encryption = true, admin-defined recipients)
- Label IDs จริงมาจาก tenant ห้าม hardcode ชื่อหรือ GUID ตัวอย่างเป็น production mapping
- Report metadata มี business owner, purpose, dataset version และ rule version ใน application DB ไม่อ้างว่าเป็น Purview catalog
- Entra sign-in, role-based reports + document ACL, asynchronous export/copy, job status, authorized download และ audit/evidence view
- Library ใช้ UDP label อีกหนึ่งตัวและ rights profile เปิด/แก้ไข/บันทึกเฉพาะ requester; source ไม่ติด MIP protection; ดู docs/10 สำหรับ recipient isolation/ownership
- Failures ต้องไม่มี fallback ให้ดาวน์โหลด plaintext
- Real MIP และ simulated mode แยกชัด; simulated mode ใช้ทดสอบ workflow แต่ไม่มี artifact download

## Functional requirements

| ID | Requirement | Acceptance |
|---|---|---|
| FR01 | Tenant-restricted sign-in | foreign tid ถูกปฏิเสธ แม้ object ID หรืออีเมลเหมือนกัน |
| FR02 | Report authorization | PayrollExporter เท่านั้นที่ขอ payroll ได้; ซ่อนปุ่มอย่างเดียวไม่พอ |
| FR03 | Server-owned classification/recipients | Request รับ report ID/parameters หรือ document ID/version เท่านั้น ไม่รับ labelId, owner, recipient, data path |
| FR04 | Context snapshot | ก่อนสร้าง job บันทึก report definition, dataset, rule version, expected label ID และ protection expectation |
| FR05 | Durable export | สร้าง job + request audit event อย่าง atomic; refresh/restart ไม่ทำ job หาย |
| FR06 | Idempotency | key เดิม+payload เดิม+principal เดิม → job เดิม; payload ต่าง → 409 |
| FR07 | Generate | สร้าง XLSX ใน private staging; browser เข้า raw path ไม่ได้ |
| FR08 | Apply | File SDK commit ไป output ใหม่ ไม่แก้ไฟล์ที่กำลังอ่าน |
| FR09 | Verify | เปิด final output ด้วย handler ใหม่ เทียบ label ID, actual protection และ artifact hash กับ expectation |
| FR10 | Release | Ready เฉพาะ live mode ที่ verify ผ่านและ evidence/state/audit ถูกบันทึกสำเร็จ |
| FR11 | Download | requester+tenant+report role ยังผ่าน, job Ready, ยังไม่หมดอายุ, artifact/evidence ตรงกัน |
| FR12 | Audit | เก็บ transition, attempts, identity, hashes, evidence, failures และ download outcome โดยไม่มี report content/token |
| FR13 | Retention | Expired ต้องดาวน์โหลดไม่ได้ก่อนลบไฟล์; cleanup ตรวจ path และบันทึกผล |
| FR14 | Fail closed | label หาย/disabled/ผิด config, SDK error, audit error, verification mismatch → ไม่ Ready |
| FR15 | Mode visibility | มี badge Live MIP / Simulated; simulated job สถานะ Simulated ไม่มี download |
| FR16 | Evidence accuracy | แยก metadata verification, encryption observation และ manual client access test |
| FR17 | Document listing/owners | reader เห็น current Published/Eligible documents ตาม ACL; owner O1/O2 เห็น drafts/lifecycle ของตนผ่าน owner scope |
| FR18 | Per-user issuance | สร้าง copy ใหม่จาก pinned source version ให้ requester เป็น ordinary recipient เพียงคนเดียว ไม่สะสมคนก่อน |
| FR19 | Recipient identity | derive RMS address จาก trusted mapping ที่ bind tid/oid; reject caller-supplied recipient; missing/stale/ambiguous → fail |
| FR20 | Mutual file denial | A เปิด/แก้ไข/บันทึก copy A; B เปิด copy A ไม่ได้ และกลับกัน ทดสอบแยกจาก web IDOR |
| FR21 | Least edit/save rights | independent output rights/recipient inspection; ไม่ใช้ built-in Editor โดยสมมติว่าไม่มี Export/Copy |
| FR22 | Source integrity | source blob version/hash, owner and ACL snapshot; no source mutation; disabled/withdrawn source ไม่ release |
| FR23 | Azure hosting | prepare IaC/resource/cost/runbook; G5 ต้อง deploy และทดสอบจริงหลัง target/cost authorization พร้อม |
| FR24 | Cloud artifact integrity | pin output blob version/ETag/hash; SQL/Blob partial failure ไม่ทำให้ candidate downloadable |
| FR25 | Owner upload | Publisher สร้าง document แล้ว owner derive จาก validated session; upload/update ของผู้อื่นถูกปฏิเสธ |
| FR26 | Quarantine and validation | ≤10 MiB unprotected XLSX, scan/structure/hash evidence ก่อน Eligible; error/unknown/fake ไม่มี live publish |
| FR27 | Owner metadata/ACL | title/purpose และ allowlisted same-tenant requesters; If-Match + transactional audit; ไม่รับ owner/label/rights override |
| FR28 | Versioning and publish | upload ใหม่เป็น draft version; old published version ใช้ต่อจน explicit publish ใหม่สำเร็จ; ไม่ overwrite source bytes |
| FR29 | Withdraw | หยุด reader visibility/new issuance/portal download หลัง commit; ไม่อ้าง revoke downloaded copies |
| FR30 | Intake lifecycle evidence | scanner/validation bind measured source hash, engine/profile versions และ immutable source binding; partial failure/orphans ไม่เผยแพร่ |
| FR31 | Retired by ADR-012 | The former interactive renderer requirement is removed; do not count its removal as a security gate pass |
| FR32 | Searchable accessible workspace | Authorized HTML list, owner filter and detail/actions; keyboard, reduced motion, responsive reflow and clear failure states |
| FR33 | React/.NET same-origin session | compiled React + .NET API/BFF ในเว็บ release เดียว; server cookie/CSRF/API401/403, no client MIP credentials and identity-cache isolation |

## UI flows

1. **รายงาน:** แสดงประเภท, owner/purpose, classification ที่คาดหวัง, dataset version และ mode; ข้อมูล preview เป็น synthetic และอยู่ภายใต้ role เดียวกับ report
2. **ขอ export:** เลือก report และ period จาก allowlist → แสดงเหตุผลที่เลือก label → submit พร้อม anti-forgery
3. **สถานะ:** Queued/Preparing/Labeling/Verifying/Publishing/Ready/Failed/RetryPending/Expired/Simulated พร้อมข้อความไทย; ไม่แสดง raw exception
4. **รายละเอียด job:** requester, processor identity, rule, expected/observed label/protection, checksum, expiry, mode และ correlation ID
5. **ประวัติ:** requester เห็นงานตน auditor เห็น metadata ทุกงานใน tenant; ไม่มีปุ่ม relabel/decrypt/download ข้ามเจ้าของ
6. **คลังเอกสาร:** ชื่อ, owner, purpose, source version และสิทธิ์ขอสำเนา; กด “ขอสำเนาสำหรับฉัน” ไม่มีช่องกรอกผู้รับ/label/owner
7. **รายละเอียดสำเนา:** source owner แยกจาก requester/protection owner, rights profile, observed recipient evidence และเงื่อนไขว่าคนอื่นเปิด copy นี้ไม่ได้ตาม ordinary-user test
8. **เอกสารของฉัน:** Publisher สร้าง/อัปโหลด ดูผลตรวจ แก้ metadata/ACL เพิ่ม version และ publish/withdraw เฉพาะของตน; reader ไม่เห็น draft/quarantine

## Nonfunctional targets — design targets, not measured results

- App limit: 10 MB final XLSX, ไม่เกิน 1,000 synthetic detail rows; เกิน limit → Failed
- Upload limit 10 MiB file/12 MiB request, intake queue cap 20 และ parser/scan bounds ตาม docs/12; limits นี้เป็นของแอปไม่ใช่ SDK
- At most 1 active worker job และ 20 queued jobs; admission overflow → 429 พร้อม Retry-After
- Job target ≤ 60 วินาทีภายใต้ warm cache/เครือข่ายปกติ; วัดจริงใน G4 ไม่ใช้เป็น SLA
- Attempt timeout 120 วินาที, maximum 3 attempts สำหรับ transient failures; lease heartbeat ต้องรองรับ native call ที่ยกเลิกไม่ได้
- Artifact TTL 60 นาทีจาก Ready; staging cleanup หลังจบ attempt ภายใน 5 นาที; cleanup scan ทุก 5 นาที
- Audit metadata retention 30 วันใน demo; ไม่ใช่ regulatory retention requirement
- All comparisons use UTC; UI แสดง Asia/Bangkok พร้อม timezone
- Host reboot ต้องกู้ job ได้โดยไม่ปล่อย output ที่ยังไม่ผ่าน verify
- Frontend bundle/load/reflow targets in docs/15 are G7 design targets, not measured SLA. Core tasks use accessible HTML controls.

## Out of scope

PDF/CSV, unrestricted file types/bulk import, content classification AI/OCR/SIT engine, policy administration UI, caller-selected arbitrary recipients, external/B2B recipients, DKE, relabel/decrypt/reprotect of already protected originals, owner transfer/approval workflow, retention labels, content marking, Data Map/Unified Catalog, SharePoint connectors, multi-tenant SaaS, mobile viewers, legal compliance certification และ performance SLA

ไม่มีการใช้ username/password flow หรือ tenant-wide decryption permission เพื่อทำให้ demo ผ่าน

## Definition of done

ผ่าน G1–G7, docs/08 tests/U01–U12/I01–I16/X01–X12 พร้อม React list/detail/session/device evidence และ live upload→publish→A/B rights demo. Missing live evidence remains unresolved; frontend simplification does not pass native/security/cloud gates.
