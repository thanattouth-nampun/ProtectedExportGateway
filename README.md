# Protected Export Gateway — v4.3

26 October checkpoint: [read-only quarantine ownership diagnostic](docs/quarantine-ownership-checkpoint.md), [actual evidence](docs/evidence/2026-10-26-quarantine-ownership.md). Live SDK rejected the B-uploaded envelope with RMS_OWNER_MISMATCH; 203 tests passed. No persisted verdict/Publish/download or tenant/Azure changes. Scanner/content-validation/promotion gates remain pending.

21 October local checkpoint: [guarded quarantine receipt](docs/quarantine-upload-checkpoint.md) supports synthetic byte receipt/status in Development only. Owner/scan/validation eligibility and production uploads remain blocked; local storage hashes are not Azure Blob-version evidence. [Actual checks](docs/evidence/2026-10-21-quarantine-upload.md).

20 October access update: [ADR-014 self-service tenant Member workspaces](docs/member-self-service-workspace.md). Every verified internal Member can manage own private drafts without individual Publisher enrollment. Protected intake/ownership/release gates remain; local tests and the subsequently approved Entra/local rollout are separate evidence. Fresh Member-session acceptance is tracked in the linked rollout record.

ขอบเขตล่าสุด: [ADR-013 standalone owner-only source](docs/standalone-owner-only-mvp.md) — พัก SharePoint/OneDrive/content connectors รับโจทย์ต้นฉบับ synthetic XLSX ที่ป้องกันให้เจ้าของเดิม แล้วออกสำเนาเฉพาะผู้รับที่ portal ACL อนุญาต โดยคง RMS owner เดิม Entra/MIP/Azure และ HR Reports ยังอยู่ใน scope การรองรับ protected input ยังเป็น feasibility spike; runtime เดิมไม่เปิดรับหรือดาวน์โหลดแทนหลักฐานจริง ดู [runbook/approval packet](docs/owner-only-spike-runbook.md) ข้อกำหนด unprotected-source/service-owner ด้านล่างเป็น baseline เก่าที่ ADR-013 supersede เฉพาะ Library path

ปรับปรุง: 19 ตุลาคม 2026 (Asia/Bangkok)
20 October checkpoint: [Owner Draft/ACL workspace](docs/owner-workspace-checkpoint.md) now has SQL-backed metadata APIs, transactional document audit and React forms. Local SQL/HTTP/browser checks are recorded separately from live Entra login, which awaits approval of a dedicated web registration. Protected intake/publication/download remain blocked.
สถานะปัจจุบัน: standalone owner-only feasibility preparation; M0/M1/live MVP ยังไม่ครบ ดู [setup](docs/developer-setup.md) และ [current evidence](docs/evidence/2026-10-19-owner-only-preparation.md).

Backend checkpoint ล่าสุด: [durable SQL queue](docs/sql-queue-checkpoint.md) พร้อม EF migrations, requester isolation, idempotency, leases/fencing และ transactional audit ที่ทดสอบบน LocalDB จริง ยังไม่เชื่อม Owner API/worker หรือเปิด download.

UI experiment ล่าสุด: [Folder tree + File list + Details](docs/folder-navigation-preview.md) พร้อม synthetic preview ที่ `spike.html`; SharePoint/OneDrive ยังไม่เชื่อมต่อ ดู [ผลตรวจ](docs/evidence/2026-10-19-folder-tree.md).

ADR-012: ผู้ใช้ยกเลิก 3D แล้ว ใช้ searchable HTML document list พร้อม Owner filter และ details/actions. React/TypeScript/.NET และ functional/security/Azure requirements ยังคงเดิม. ผลงานและหลักฐาน renderer เก่าเป็นประวัติย้อนหลัง ไม่ใช่ deliverable ปัจจุบัน.

Frontend baseline: **React + TypeScript + Vite + React Router/Tailwind/query/forms** ตาม [current assessment / ADR-012](docs/15-long-term-stack-assessment.md). Build static React assets ใน .NET API/BFF web release/origin เดียว; Windows MIP worker แยกตามเดิม. Node ใช้ build/CI เท่านั้น.

Visual direction: calm HTML list/detail workspace, readable owner/version/status, accessible forms and responsive layout. 21st.dev ใช้คัด components รายตัวหลังตรวจ original source/license/dependencies/accessibility.

[ต้นแบบเดิม v4](prototype/dist/index.html) เป็น legacy flow reference เท่านั้น ไม่เชื่อม Entra/upload/AV/MIP/Azure และไม่ใช่ integration evidence.

## เป้าหมายที่ผู้ใช้เลือก

เว็บเดียวมีสองส่วน:

1. **Reports:** สร้างรายงาน HR จำลองเป็น XLSX และติด label/protection ตามประเภทรายงานก่อนดาวน์โหลด
2. **Document Library:** Document Publisher อัปโหลด XLSX และเป็น Owner จาก Entra ID จัดการรายละเอียด/ACL/version แล้วเผยแพร่หลังตรวจไฟล์ ผู้ใช้ล็อกอินขอสำเนาที่ป้องกันเฉพาะตนด้วย UDP label ได้

วงจรเอกสาร: **สร้าง Draft → Upload ไป private quarantine → สแกน/ตรวจโครงสร้าง → Owner Publish → ผู้ใช้ขอสำเนา → MIP protect/verify → Download** Owner เพิ่ม version หรือ Withdraw ได้ตามสิทธิ์ ดู [upload lifecycle](docs/12-document-ingestion-and-publishing.md)

ตัวอย่างเอกสารเดียวกัน: A ดาวน์โหลดได้สำเนา A, B ดาวน์โหลดได้สำเนา B; แต่ A เปิดสำเนา B ไม่ได้ และ B เปิดสำเนา A ไม่ได้ ใช้ label ID เดียวกันได้โดยสิทธิ์แต่ละสำเนาไม่สะสมรวมกัน

คำว่า Editor ใน requirement หมายถึงเปิด/แก้ไข/บันทึกได้ ต้องพิสูจน์ exact rights ใน client ไม่ใช้ permission level Editor สำเร็จรูปโดยไม่ตรวจ เพราะอาจรวม Copy/Export ที่ไม่ตรงเป้าหมาย ดู [per-user specification](docs/10-per-user-document-download.md)

## Hosting บน Azure เป็นส่วนหนึ่งของเป้าหมาย

Baseline: **Azure App Service สำหรับเว็บ + Azure Windows VM สำหรับ MIP worker + Azure SQL Database + private Blob Storage + Key Vault + Azure Monitor** พร้อม managed identities, VNet/private connectivity และ cost/deployment runbook

เหตุผลที่แยก worker ไป VM คือให้ควบคุม native SDK dependencies/lifecycle ได้ก่อน ไม่ได้อ้างว่า App Service ใช้ MIP ไม่ได้ การเปลี่ยน worker hosting ต้องผ่าน compatibility gate ต่างหาก

ยังไม่มีการสร้าง resource, เปิด billing หรือ deploy โดยเอกสารนี้ ขั้น provisioning ต้องเลือก subscription/region/SKU/cost envelope จริงก่อนดำเนินการ

## การนำไปเริ่ม repo

เริ่มจาก [START_HERE.md](START_HERE.md), [AGENTS.md](AGENTS.md), docs/01–15 และ prototype README. ทำตาม docs/07 พร้อม SDK/scanner/SQL/web acceptance gates; ห้ามนับ static/fake evidence เป็น live success.

| เอกสาร | เนื้อหา |
|---|---|
| [01-product-requirements.md](docs/01-product-requirements.md) | สอง modules, personas, requirements และขอบเขต |
| [02-system-architecture.md](docs/02-system-architecture.md) | Azure topology, processing และ verified release |
| [03-tech-stack-and-decisions.md](docs/03-tech-stack-and-decisions.md) | Stack, hosting และ identity decisions |
| [04-environment-and-feasibility.md](docs/04-environment-and-feasibility.md) | G0–G7 prerequisites และ proving checks |
| [05-api-and-data-model.md](docs/05-api-and-data-model.md) | HTTP/data/config contracts |
| [06-security-and-operations.md](docs/06-security-and-operations.md) | Threat model, permissions, recovery และ cleanup |
| [07-implementation-plan.md](docs/07-implementation-plan.md) | Tasks และ dependencies |
| [08-validation-and-demo.md](docs/08-validation-and-demo.md) | Test matrix และ showcase script |
| [09-research-and-open-questions.md](docs/09-research-and-open-questions.md) | Sources, evidence status และ open decisions |
| [10-per-user-document-download.md](docs/10-per-user-document-download.md) | สิทธิ์เฉพาะคน, ownership, recipient isolation และ tests |
| [11-azure-hosting-and-resources.md](docs/11-azure-hosting-and-resources.md) | Azure resources, RBAC/network, cost และ deployment plan |
| [12-document-ingestion-and-publishing.md](docs/12-document-ingestion-and-publishing.md) | Owner upload, quarantine, scanner, metadata/ACL, versions และ publish/withdraw |
| [13-ux-ui-design.md](docs/13-ux-ui-design.md) | Visual direction, role-based IA, screen flows, English UI copy, Inter typography, states, responsive/accessibility and X01–X12 |
| [14-component-inventory.md](docs/14-component-inventory.md) | 21st.dev shortlist/licenses, React compatibility และ component adoption rules |
| [15-long-term-stack-assessment.md](docs/15-long-term-stack-assessment.md) | comparison, adopted stack, same-origin BFF, HTML workspace/session/load/accessibility boundaries and G7 |
| [Prototype README](prototype/README.md) | วิธีเปิดและขอบเขตการทดสอบต้นแบบ |

## ขอบเขตและสิ่งที่ยังไม่พิสูจน์

- Single tenant, synthetic data, unprotected XLSX เท่านั้น; Owner upload ผ่านเว็บเป็นเส้นทางหลัก Seed tool ใช้ทดสอบและต้องผ่าน validation contract เดียวกัน
- Business owner ใน library ไม่เท่ากับ RMS protection owner; baseline candidate ใช้ service principal เป็น protection owner ไม่ delegate ownership ให้ downloader
- Graph ไม่เป็น dependency บังคับ แต่ identity-to-RMS-address mapping ต้องผ่าน validation; mapping ที่ขาด/ไม่ชัดเจนต้อง fail closed
- Worker concurrency 1; Azure SQL เป็น authoritative job queue ไม่มี Service Bus ใน baseline
- เพิ่ม intake queue และ quarantine container; worker มี source-promotion capability ที่ควบคุมด้วย immutable version/hash ไม่ใช้ข้อสมมติ worker read-only จาก v2
- Candidate scanner ใช้ Microsoft Defender Antivirus ผ่าน scoped helper บน Windows VM ต้องผ่าน G6; MIP ไม่ใช่ antivirus และ exit code 0 ของ scanner อย่างเดียวไม่ใช่หลักฐาน Clean
- Report downloads และ document copies ใช้ release/evidence pipeline ร่วมกัน แต่มีคนละ protection profile
- Local development ใช้ SQL Server-compatible database และ private filesystem adapters; SQLite ไม่ใช่ baseline ของ distributed Azure deployment
- React source/build and .NET source have separate tooling but one web release; exact versions/build/auth/rendering/performance still need G7 validation. Existing native/rights/scanner gates remain unresolved until tested
- Live SDK/app-only custom protection, exact edit/save rights, recipient readback, Azure runtime, policy refresh และ access tests ทั้งหมด NOT RUN
- Upload/scanner/publish lifecycle และ G6 ทั้งหมด NOT RUN; ไม่เผยแพร่ live จาก fake scanner หรือผล unknown/error
- Simulated mode ไม่มี artifact download และไม่นับเป็น live success
- ไม่อ้างว่าผู้มีสิทธิ์อ่านไม่สามารถถ่ายภาพหรือถ่ายทอดข้อมูล หรือ privileged RMS/service accounts ถูกปฏิเสธเหมือน ordinary users

Baseline v4.2 retains React and .NET and removes the renderer under ADR-012. Withdrawal does not revoke already downloaded files. Live MIP/scanner/Office/Azure gates remain unresolved.

Approved theme: warm-white background #F7F8F5, white surfaces #FFFFFF, charcoal text #202923, forest-green primary #24634B and soft-mint selection #E5F0E9. Apply semantic tokens and state rules from docs/13; preserve English-only UI and Inter typography. Earlier blue-primary proposals are superseded.
