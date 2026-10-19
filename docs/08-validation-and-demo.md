# 08 — Validation, evaluation and demo v4.2

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

## Evidence levels

| Level | What it proves | What it does not prove |
|---|---|---|
| Domain tests | mapping/state/release logic under test inputs | tenant behavior |
| Integration with fake | durable queue/auth/download denial/fault handling | MIP labels/encryption |
| Live SDK inspection | label/protection observation of exact artifact | recipient can/cannot open content |
| Client access control | observed allowed/denied behavior in tested client/config | all clients/channels, future policy changes |
| Pilot usage | observed workflow/value feedback | general market demand or legal compliance |
| Azure deployed tests | actual compute/SQL/Blob/MI/networking behavior on selected resources | other regions/SKUs or production scale |
| Live AV/intake tests | observed scan/validation/publication result for exact upload bytes and tested lifecycle | universal malware absence or compliance certification |

UI label display, file extension, unreadable ZIP header หรือ hash เพียงอย่างเดียวไม่ใช่หลักฐานว่าการเข้ารหัส/rights ถูกต้อง

## Test matrix

| ID | Case | Expected | Level |
|---|---|---|---|
| V01 | summary mapping | expected internal label, no personal payroll fields | unit + XLSX integration |
| V02 | payroll mapping | expected confidential label + protection=true | unit + live |
| V03 | caller injects label/tenant/path | reject; no override | API integration |
| V04 | missing payroll role | 403; no job created | auth integration |
| V05 | foreign tenant / wrong requester | cannot access job or download | auth integration |
| V06 | auditor tries other person's download | denied despite audit metadata access | auth integration |
| V07 | repeated key/same request | same job, no duplicate work | concurrent DB integration |
| V08 | same key/different request | 409 | DB integration |
| V09 | unknown/disabled label | Failed; no download | fake fault + live controlled label/config change |
| V10 | commit success but inspect mismatch | Failed; candidate inaccessible | integration fault |
| V11 | encrypted expected but observed false/unknown | Failed | integration + inspect semantics evidence |
| V12 | fake reports success | Simulated; no download route succeeds | integration |
| V13 | runtime SDK/network failure | bounded retry/Failed; no raw output | integration + live failure |
| V14 | stale policy/refresh failed | stop export; no Ready | integration + live refresh evidence |
| V15 | DB/audit failure before Ready | no Ready; orphan not exposed | failure injection |
| V16 | file modified after verification | download blocked/quarantined | filesystem integration |
| V17 | expiry boundary | new downloads rejected even before delete | controlled clock integration |
| V18 | cleanup lock/ACL failure | expired stays inaccessible, retry recorded | filesystem integration |
| V19 | reboot/crash in each stage | restart/attempt isolation; no unverified release | process integration |
| V20 | stale worker after lease takeover | cannot commit Ready | DB concurrency integration |
| V21 | stuck native call | supervisor terminates attempt process; bounded retries; web stays alive | process/live environment |
| V22 | raw/staging/candidate HTTP path | inaccessible | web integration |
| V23 | missing CSRF token | mutation denied | web integration |
| V24 | confidential allowed user opens file | real content readable | manual supported client |
| V25 | confidential denied user opens same file | rights-based denial with successful allowed control | manual isolated client identity |
| V26 | label-only summary opens | label present; no encryption claim | live SDK + client |
| V27 | shutdown during download/expiry/cleanup | no new unauthorized download; event semantics accurate | process integration |
| V28 | queue full and simultaneous admission | capacity bound respected; 429 | concurrent integration |
| V29 | placeholder live configuration | startup/readiness fails; cannot release | configuration integration |
| V30 | repository/logs secret scan | no raw tokens/key/cache/report contents | static/manual inspection |
| V31 | Library per-user source/rights/recipient checks | U01–U12 in docs/10 all pass at specified levels | unit/integration/live/client |
| V32 | Blob mutable latest/version substitution | exact source/output version only; mismatch denied before response body | Azure integration |
| V33 | Upload succeeds/SQL Ready transaction fails | private orphan inaccessible then cleanup; never raw fallback | Azure fault injection |
| V34 | MI scoped access | web only quarantine-upload/export-read; no source access/export-write; worker reviewed promotion/cleanup, cannot use create-only app flow to overwrite source | Azure live negatives + app/source integrity |
| V35 | App Service recycle/VM restart or hung native call | durable jobs/no duplicate recipient leakage/no unverified release | Azure process |
| V36 | Key Vault/private DNS/SQL/Blob unavailable | safe failure/unavailable, no success artifact | Azure controlled failure |
| V37 | Cloud logs/budget/retention settings | no secret/report content, budget notifications configured, retention costs known | Azure operator review |
| V38 | Owner upload/publish lifecycle | I01–I16 in docs/12; G6 live proof required, fake scan no live publishing | role/DB/Blob/AV/parser/process |
| V39 | scanner exit0/remediation/unknown and altered input | no Eligible/Published; reliable engine facts bound to hash | AV/helper integration + controlled failure |
| V40 | upload limits/unsafe OOXML/parse timeout | reject before publication; no worksheet execution/unbounded decompression | bounded intake/parser tests |
| V41 | new version and Withdraw vs pending/Ready copy | current-publication/ACL checks deny stale access; already sent bytes not claimed revoked | DB/API/process races |
| V42 | Authorized list search/owner filter/selection/details | Equivalent authorized records and actions; keyboard access; no hidden metadata | React/browser |
| V43 | UI/data failure and reduced motion | Clear recovery, usable forms/list/status without lost authorized state | Browser |
| V44 | BFF session/CSRF/401/403/SPA routes and restart | no browser MIP/Entra tokens, API errors not HTML fallback, valid auth/CSRF/cookie behavior | backend + browser + Azure |
| V45 | Identity change/cache and bundle/load budgets | No old-principal metadata in selection/query/in-flight state; named browser/device measurements | G7 |

Tests ที่ใช้ HTTP test identities ต้องแยกจาก real OIDC smoke test; test auth handler ห้ามเปิดใน non-test deployment

## Manual access procedure

1. เตรียม allowed และ denied identities ใน test tenant ตาม label recipients ที่บันทึกจริง ไม่ใช้ service principal เป็น denied control
2. Generate live confidential artifact หนึ่งไฟล์ จด output hash และ run/job IDs
3. Allowed user เปิดใน supported Excel client ให้เห็นเนื้อหาจำลองและ label
4. ทดสอบ denied user ใน profile/OS account/เครื่องที่แยก credentials และ Office/RMS caches ชัดเจน Browser private window อย่างเดียวไม่รับประกันแยก desktop Office identity
5. ส่งไฟล์เดียวกันผ่าน controlled test mechanism; deny ไม่ใช่แค่ “ดาวน์โหลดจากเว็บไม่ได้” เพราะกำลังทดสอบ file protection อีกชั้น
6. เก็บผล rights-based denial พร้อม allowed control/network/client context หาก error ไม่ชัดให้ผล INCONCLUSIVE ไม่ใช่ PASS
7. ลบไฟล์ทดสอบตาม demo procedure; copy ที่ออกไปแล้วไม่ถูกลบตาม gateway artifact TTL

ไม่ต้องทดสอบ arbitrary external recipients หรือ bypass สิทธิ์เพื่อให้ demo น่าตื่นเต้น

Library เพิ่ม bilateral checks: A เปิด/แก้ไข/บันทึก copy A, B เปิด/แก้ไข/บันทึก copy B, A เปิด B ไม่ได้และ B เปิด A ไม่ได้ ทดสอบ saved protected copies ซ้ำด้วย ตรวจ ordinary-user rights แยกจาก processor owner และพิสูจน์ว่า rights profile ไม่มี unintended Copy/Export ที่ขัด requirement

## Metrics

- Label correctness: passed expected label IDs / successful live export outputs; fixture scenarios ที่ทดสอบควรตรงทั้งหมด
- Protection correctness: expected-vs-observed protection state ของทุก output ที่ Ready; unknown = fail
- Release control: negative scenarios ที่ไม่มี unauthorized artifact download / negative scenarios ทั้งหมด เป้าหมาย 100% ใน test matrix ไม่ใช่ security guarantee ทั่วไป
- Access behavior: allowed and denied controls พร้อม conclusive evidence; ไม่มี denominator ที่รวม untested users
- Performance: per-stage cold/warm duration, file size, SDK/version, network context; รายงาน median และ range สำหรับ sample เล็ก ไม่อ้าง p95 ที่น่าเชื่อถือจากตัวอย่างน้อย
- Business pilot: จำนวนขั้นตอน manual ที่ลดลง, failure visibility, เวลาหาสาเหตุ export failure และ feedback จาก HR/IT ก่อน–หลัง ไม่แต่งตัวเลข ROI

## Demo script — เป้าหมายประมาณ 15–20 นาที ไม่ใช่ benchmark

1. อธิบายปัญหารายงานหลุด label และแสดง mode=Live MIP
2. เปิด summary fixture แสดง business purpose/owner และ export; เห็น internal label-only พร้อมไฟล์ผลลัพธ์
3. เข้าด้วย PayrollExporter ขอ payroll; อธิบาย server mapping และ status pipeline
4. เปิด evidence แสดง expected/observed label, protection, requester เทียบ processor, file hash และ TTL
5. Allowed user เปิด confidential XLSX สำเร็จ
6. Denied user เปิดไฟล์ hash เดียวกันแล้วถูกปฏิเสธตาม rights พร้อมอธิบาย client isolation
7. ตั้ง controlled failure เช่น invalid mapped label ใน test config หรือหยุด dependency ตาม procedure; export ล้มเหลวและไม่มี plaintext download
8. O1/O2 มี DocumentPublisher: สร้าง Draft/upload synthetic XLSX, owner derives from login, ระบุ metadata/allowlisted ACL A/B; แสดง quarantine/scan/validation status แล้ว owner publish เอง; ไม่มี direct original download
9. B login ขอ source document เดียวกันได้ own copy; แสดง label เดียวกันแต่ artifact/recipient rights ต่างกัน
10. Cross-open A/B copies รวม saved copies ด้วย isolated client credentials; ต้อง rights-based deny ไม่มี All Employees grant หรือ downloader-as-owner
11. แสดง Azure topology, App Service endpoint, VM worker heartbeat, SQL job evidence และ private Blob binding โดยไม่เผย secrets
12. Owner upload version ใหม่ แสดงว่าของเดิมยัง Published; publish ใหม่แล้ว current pointer เปลี่ยน; ลอง stale version request ต้องถูกปฏิเสธ
13. Withdraw document; reader catalog/new requests/portal downloads ของเอกสารนั้นถูกปิด; อธิบายว่า existing downloaded copy ไม่ถูก revoke อัตโนมัติ
14. แสดง audit ว่าเป็น application audit; ชี้ scan evidence vs MIP evidence, ownership/rights/non-revocation/cost/hosting limits
15. Search/filter the authorized HTML library, select a document and inspect owner/details/actions. Demonstrate keyboard/reflow/reduced-motion/error states and identity-cache clearance.

หาก environment live มีปัญหา ให้แสดง recorded evidence ที่ระบุวันที่/versions หรือ simulated UI โดยติดป้ายตามจริง ห้ามเปลี่ยนเป็น fake ระหว่าง demo โดยไม่แจ้ง

## Release acceptance checklist

- G1–G7 ผ่านตามหลักฐาน; U01–U12/I01–I16/X01–X12 and V31–V45; historical/static prototype cannot prove current integration; unresolved critical items closed
- Live SDK และ manual recipient evidence แยกชัด
- Negative tests ของ release/auth/recovery ผ่าน
- Commands/setup ที่ส่งต่อทดสอบได้บน clean supported environment หรือระบุส่วนที่ยังไม่ทดสอบ
- ค่าใช้จ่าย/license/IPIA/public distribution ตรวจตาม target จริง
- ไม่มี real employee data, raw credentials/caches หรือ claim ว่า production-ready
