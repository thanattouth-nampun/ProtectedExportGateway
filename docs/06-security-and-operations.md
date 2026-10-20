# 06 — Security, privacy and operations v4.2

> ADR-014 access update (20 October 2026): [Self-service Member workspaces](member-self-service-workspace.md) supersedes Publisher-only/pre-enrolled-owner empty-draft access below. Every verified internal tenant Member gets own private drafts; source RMS-owner mapping, reviewed recipients, scan/release and report/auditor controls remain. Approved tenant claim/enrollment rollout and live acceptance are tracked separately in ADR-014.

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

## Threat model

| Threat | Required mitigation | Proof |
|---|---|---|
| เปลี่ยน report/role/tenant จาก request | validated OIDC session + server authorization + strict DTO | foreign tenant/role/injected field tests |
| IDOR ขโมย job/download | query scoped ด้วย tenant+requester; auditor metadata only | requester B ใช้ job A ไม่ได้ |
| ลดระดับ label ผ่าน input | mapping server-owned; ไม่มี relabel endpoint | injected labelId ถูก reject |
| SDK ล้มเหลวแต่ปล่อย plaintext | no release until sealed artifact verified | fault injection ทุก pipeline stage |
| เปลี่ยนไฟล์หลัง verify | immutable artifact ID, ACL, handle lock + rehash ก่อน stream | modified artifact ถูกบล็อก |
| Stale worker ปล่อยผลทับ | DB fencing/rowversion และ isolated attempt paths | stale lease cannot commit Ready |
| Raw/static URL รั่ว | runtime อยู่นอก webroot, no direct file hosting | HTTP raw path inaccessible |
| Secret/policy cache รั่ว | certificate store/secret provider, private ACL, gitignore/redaction | repository/log scan |
| Arbitrary path/SSRF | bounded upload filename is display only; server storage/helper tokens; no caller URL/path | traversal/injection negative cases |
| XLSX formula/link injection | fixture-only, text cells explicit, no macros/external links | inspect generated workbook |
| Expired artifact ยังเข้าถึงได้ | authorize TTL ทุก requestก่อน stream; cleanup independent | clock-driven expiry test |
| Tenant policy freshness ไม่ทราบ | validated refresh strategy + max age fail closed | stale/refresh failure test |
| Audit unavailable | Ready transaction fails; download requires audit write before stream | DB/audit outage tests |
| Cookie theft/CSRF | HTTPS, Secure/HttpOnly cookies, proper SameSite/OIDC handling, anti-forgery | sign-in and mutation tests |
| Copy permissions leak across users | fresh per-job descriptor + scoped idempotency/cache + independent recipient/rights readback | U01/U03/U06/U12 |
| Identity email spoof/missing mapping | bind validated tid/oid to trusted RMS address; fail closed | U04/U05 |
| Source update/Blob substitution | pinned source/output versions + conditional read + SHA256 | U09 + cloud integrity tests |
| Cloud privilege escalation | separate web/worker MIs, scoped data-plane rights, no source read for web | G5 RBAC denial tests |
| Owner impersonation/unauthorized publish | validated session owner + Publisher role + If-Match + atomic audit | I01/I02/I08/I12 |
| Malicious/unvalidated uploads | private quarantine, reliable scan evidence, bounded isolated OOXML validator | I04–I07; no unknown/fake live publish |
| False clean scanner result/elevated command abuse | scoped helper fixed command/path token, verify verdict semantics/hash; exit0 alone insufficient | G6 + I06 |
| Publish/version/withdraw TOCTOU | current published pointer/ACL check at issue/release/download; no source overwrite | I10/I11 |
| SPA auth/CSRF/HTML fallback confusion | server session, antiforgery header, structured API401/403; no SPA fallback for /api,/auth | G7 |
| Cached private metadata across identity | clear query/selection/in-flight state; no user data in assets/offline caches | G7 |
| UI leaks hidden documents/content | Server-authorized list/options/counts only; no global hidden dataset | G7 + ACL negatives |
| UI/data loading failure blocks work | Accessible error/retry states and preserved authorized forms/job state | G7 |

## Identity baseline and limits

แยก web app registration จาก processor service principal requester role ใช้อนุญาตธุรกรรมในเว็บ ส่วน RMS rights กำหนดการอ่านไฟล์ ทั้งสองต้องตรวจคนละชั้น

Candidate processor permissions คือ tenant policy read และ content writer ตาม official documentation [S02] ต้องผ่าน G1 ก่อนอนุมัติว่า setup ใช้งานได้ ไม่มี Content.SuperUser เป็น default baseline

Web sign-in ไม่จำเป็นต้องมี Graph application permission เพราะ MVP ไม่มี Graph API dependency ใช้ app role assignments ที่ผู้ดูแลกำหนด role changes/session revocation อาจมี propagation delay ตาม session/identity platform ต้องบันทึก observed delay ไม่อ้าง immediate revocation

React doesn't replace server authorization. No Entra/MIP tokens in localStorage/sessionStorage by default. Same-origin BFF/CSRF per docs/15; public JS/models have no secrets/private file metadata. Authorized API/doc/download responses no-store; no offline mutation-replay service worker in MVP. XSS/CSP/encoding/dependency risks remain; HttpOnly cookie is not XSS immunity

เลือก authorization freshness สำหรับ demo: validate session/token ตาม library, cookie absolute lifetime ≤ 30 นาที และ revalidate principal ทุก request จาก session ที่ยัง valid หากต้อง guarantee การถอน role ทันทีต้องออกแบบ online entitlement check/revocation เพิ่ม ไม่รวมใน baseline นี้

Reports ใช้ admin-defined recipients ตาม report label ส่วน DocumentCopy ใช้ UDP label + server-derived requester recipient เพียงคนเดียว ห้าม broad group/domain grants หรือสะสมคนก่อนหน้า candidate limited rights เป็น VIEW/DOCEDIT/EDIT ตาม docs/10 ไม่ใช้ built-in Editor โดยไม่ตรวจ Copy/Export การรู้ label ID ไม่ได้ให้สิทธิ์ขอเอกสารหรือเปิดไฟล์

Library owner เป็น business metadata ไม่ได้ให้สิทธิ์อ่าน copy คนอื่นโดยอัตโนมัติ Requester ไม่ควรเป็น protection owner; candidate service principal เป็น owner ตาม flow ที่พิสูจน์ G1 privileged owner/recovery accounts ต้องแยกจาก ordinary-user denial claim

Owner additionally manages its document metadata/ACL/version/publish/withdraw with DocumentPublisher role; this lifecycle authority is separate from RMS ownership. Create derives owner from session; owner transfer/arbitrary directory grants are outside MVP. Raw originals/quarantine never have download/preview routes, including for owner.

## Credential and SDK handling

- Azure Key Vault แยก web/processor credentials และ scoped managed identities; private-key-bearing certificate retrieval/signer strategy ต้องผ่าน G5; local certificate store ใช้เฉพาะ local feasibility หรือ approved VM loading strategy
- Config ใน git ใช้ placeholders; local dev secret provider ไม่ใช่ raw passwords ใน command line
- Allowlist trusted authority/tenant/resource ใน AuthDelegate ก่อน acquire token เพื่อไม่ให้ credential ถูกใช้กับ destination ที่ไม่ตั้งใจ
- Cache SDK policy/license/token แยก tenant+identity และอยู่ private data root; ห้ามบันทึก raw cache ใน evidence หรือ upload เป็น diagnostics อัตโนมัติ
- SDK auto logging/telemetry ต้อง implement consent ตาม [S03]; ไม่ใช้ checkbox ที่อ้างว่า user ยินยอมโดยไม่มีการแจ้งและการจัดการจริง
- ไม่ปิด TLS validation หรือบังคับ decrypt ถ้าผู้ใช้ไม่มีสิทธิ์
- Scanner helper elevation is isolated from non-admin web/MIP worker; no free-form command/path args. Do not create AV exclusions/disable host controls to force pass; unknown/stale engine or signature status blocks publishing. Signature age ≤24h is application target requiring actual G6 evidence

## Storage and download

VM data root มี private staging/SDK cache/temporary verified outputs; cloud sources/final candidates อยู่ private Blob และ metadata อยู่ Azure SQL ไม่ส่ง path จริงกลับ browser คำนวณ storage keys จาก server IDs ไม่ต่อ filename ของ caller Local filesystem adapter ใช้เฉพาะ development ไม่ยกเป็น cloud integrity proof

Incoming bytes go to a separate private quarantine container. Web upload identity scoped there only, worker reads/scans/validates before promoting a new source version. Reviewed source read/write-no-delete capability is new for intake; create-only logic/pinned versions detect substitution but Azure write RBAC is not write-once. No business reader or uploader gets direct Blob credentials/SAS.

Download:

1. ตรวจ auth/tenant/requester/current report role หรือ document ACL+Published/current-version/real-eligible source/state/TTL และ recipient mapping binding
2. Read exact recorded Blob version/ETag into ≤10 MB private buffer (local adapter ใช้ locked handle)
3. ตรวจ byte length/hash, recipients/rights evidence และ artifact/version binding ก่อนส่ง byte แรก; ใช้ buffer เดิมโดยไม่ fetch latest อีกครั้ง
4. บันทึก DownloadStarted สำเร็จก่อนส่ง body; ใช้ Cache-Control: no-store, Content-Disposition: attachment และ X-Content-Type-Options: nosniff
5. Stream verified buffer เดิม; record DownloadStreamCompleted หรือ DownloadStreamAborted แบบ best effort พร้อม retry ของ event หากเขียนไม่ได้

DownloadStreamCompleted หมายถึง server ส่ง stream สำเร็จตามที่สังเกตได้ ไม่พิสูจน์ว่าผู้ใช้บันทึกหรือเปิดไฟล์แล้ว ไม่ใส่ชื่อบุคคลใน filename ไม่สนับสนุน range downloads ใน MVP เพื่อลด complexity ของ integrity/evidence

TTL รับประกันการไม่เริ่ม download ใหม่หลังหมดอายุ ไม่ใช่การยกเลิกสำเนาที่ดาวน์โหลดไปแล้วหรือ stream ที่เริ่มก่อน expiry ส่วน RMS content expiry/revocation แยกจาก artifact TTL

## Cleanup and crash recovery

- Mark Expired ก่อน delete; ไม่รอ delete สำเร็จจึงปิด download
- Cleanup scan ทุก 5 นาที; stale plaintext target removal ภายใน 5 นาทีหลัง attempt สิ้นสุด; crash residue ตรวจตอน startup และ scan
- ตรวจ resolved absolute paths อยู่ใต้ configured data root ก่อน recursive operation; ไม่ลบ parent จาก unchecked ID
- File lock/deletion failure → pending deletion + retry/backoff + operator-visible alert
- Orphan candidates ไม่มี DB Ready binding จึงดาวน์โหลดไม่ได้; ลบหลังแน่ใจว่าไม่มี active lease/handle
- Audit metadata 30 วัน; artifact TTL ไม่เท่ากับ audit retention
- SSD/filesystem delete ไม่ใช่ secure physical erasure; ใช้ host disk encryption และ access control ตาม environment
- Reboot recovery ใช้ DB lease + fencing token; unknown native worker ต้องสิ้นสุดก่อนเริ่ม attempt ใหม่ supervisor ต้องถูกทดสอบ ไม่ใช่เพียง timer ที่ยกเลิก Task
- Cloud cleanup ลบ export Blob/version ตาม retention policy ที่อนุมัติ; source container แยกและ worker ไม่มีสิทธิ์แก้/ลบ originals เวอร์ชัน/soft-delete/backups อาจยังเก็บข้อมูลและมีค่าใช้จ่ายหลัง logical expiry
- App Service recycle ต้องไม่ทำ job/state หายเพราะ SQL เป็น source of truth; SQL/Blob publication ไม่มี distributed transaction ใช้ inaccessible candidates + fenced Ready commit + orphan cleanup
- Quarantine intake/source-promotion partial failures also leave inaccessible unbound objects. Cleanup target ≤24h after terminal/abandoned intake; retention exceptions/backups recorded separately. Valid historical source versions are not deleted by export/quarantine cleanup
- Publish new version/Withdraw atomically changes current pointer/revision+audit; rejects subsequent issuance/download of superseded/withdrawn versions. It cannot recall already streamed bytes or downloaded protected files. No UI claim of file revocation

## Observability

เก็บ counts ของ queued/ready/failed/retry/expired, stage duration, cleanup pending count, worker heartbeat age และ safe error categories ไม่เก็บ report bytes, tokens, private keys หรือ raw SDK policy

Application audit เป็น append-only ตาม application semantics แต่ DB owner แก้ได้ ไม่อ้างว่า immutable หรือเป็น Microsoft native audit event การใช้ external immutable journal เป็นงาน production hardening

Health ready แยก config/database/store/worker heartbeat; policy readiness มี timestamp และ safe reason ห้าม health endpoint แสดง tenant IDs/cert details สู่สาธารณะ

Add intake queue age/stage/scan verdict counts, scanner health/intelligence age, failed promotion/quarantine cleanup and publish/withdraw events. Clean result is application evidence from actual scanner run, not a native Purview detection. Do not store raw uploaded filenames/content, AV diagnostic dumps or workbook XML in ordinary logs.

## Operator runbook

| Symptom | Next check |
|---|---|
| Job ค้าง | heartbeat, lease expiry, process health; อย่าเปลี่ยน Ready ด้วยมือ |
| SDK auth error | resource/authority/certificate validity/admin consent และ processor identity |
| Label not found | tenant/engine identity, publication/scope, refresh time และ configured ID |
| Encrypted output เปิดไม่ได้ทุกคน | allowed control, rights config, client sign-in/version, network; ไม่สรุปว่า protection ทำงานถูกต้อง |
| Verification mismatch | quarantine artifact, record expected/observed, inspect SDK semantics/version |
| Cleanup ล้มเหลว | locks/ACL/disk แล้ว retry; artifact ต้องยังไม่ downloadable |
| DB unavailable | fail closed สำหรับ create/Ready/download; restore DB/store คู่กันแล้ว revalidate binding |

## Deployment boundary

Local HTTPS สำหรับ developer; Azure target เป็น App Service web + Windows VM worker พร้อม SQL/Blob/Key Vault/private networking/Monitor ตาม docs/11 ต้องเลือก ingress audience/region/SKUs/cost และตรวจ G5 ก่อน showcase บน Azure ยังไม่มี resource หรือค่าใช้จ่ายที่สร้างแล้ว ห้ามใช้ข้อมูลจริงหรือ expose สู่ audience ที่ไม่ได้กำหนดก่อนตรวจ security/terms/licensing/backup/rotation/service ownership ตามบริบท
