# 10 — Document Library and per-user protected copies v3

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

## User-approved behavior

เว็บมีไฟล์อยู่แล้วและแต่ละไฟล์มี owner ผู้ใช้ A/B sign in ด้วย Entra ID และมีสิทธิ์ดาวน์โหลดเอกสารต้นฉบับเดียวกันได้ แต่แต่ละคนได้รับ protected copy ของตน A เปิดและแก้ไข copy A ได้; B เปิด copy A ไม่ได้ และกลับกัน

นี่คือ per-file publishing permission ไม่ใช่การเพิ่มผู้ใช้เป็น editor ของ configuration กลางของ label ไม่สร้าง label ใหม่ต่อคน ไม่แก้ source file และไม่เพิ่ม A/B สะสมใน recipient list

## Demo fixtures and authorization

Owners O1/O2 ที่มี DocumentPublisher อัปโหลด synthetic XLSX เช่น `project-plan` และ `budget-working` ผ่าน quarantine/scan/validation แล้ว explicit publish ตาม docs/12 source version/hash immutable ไม่มี macros/external links/MIP-protected sources Seed tool มีไว้ fixtures/tests และห้าม bypass intake gate

Document metadata: title/purpose, session-derived businessOwner tid/oid/alias, server-fixed protection profile, publicationStatus/currentPublishedVersionId/revision และ owner-managed allowlisted ACL `CanRequestProtectedCopy` สำหรับ A/B

Owner ที่ต่างกันต้องแสดงจริงในหน้ารายการ/รายละเอียดและถูกบันทึกใน snapshot ไม่ใช้ชื่อผู้ดาวน์โหลดแทน original business owner baseline owner ดู metadata ของเอกสารที่ตนรับผิดชอบได้ แต่ไม่มีสิทธิ์ดู/download job ของผู้อื่นเพียงเพราะเป็น business owner

หาก owner ต้องการ copy ให้ผ่าน requester ACL/LibraryReader ของตนเหมือนคนอื่น Owner มี UI จัดการ metadata/ACL/version/publish/withdraw เฉพาะของตนตาม docs/12 แต่ไม่มี owner-transfer UI หรือสิทธิ์ RMS/job ของทุก copy โดยอัตโนมัติ

## Ordinary-user permission matrix

| Artifact | A | B |
|---|---|---|
| Copy A จาก source version X | เปิด/แก้ไข/บันทึกตาม profile | เปิดไม่ได้ |
| Copy B จาก source version X | เปิดไม่ได้ | เปิด/แก้ไข/บันทึกตาม profile |
| Source original ใน Blob | ไม่มี direct download | ไม่มี direct download |

Service principal/RMS protection owner และ tenant recovery/super-user capabilities ไม่ใช่ ordinary A/B recipients ต้องระบุ privileged exceptions ตาม environment จริง ไม่อ้างว่าไม่มีใครในโลกนอกจาก requester เปิดได้

## Meaning of Editor

คำว่า Editor ที่แสดงใน UI ต้องมีคำอธิบาย “เปิด แก้ไข และบันทึกสำเนาที่ได้รับสิทธิ์” ไม่ใช้ชื่อ built-in role เป็นข้อพิสูจน์สิทธิ์ Microsoft ระบุ permission level Editor รวมสิทธิ์มากกว่าการแก้ไข เช่น Copy/Export ขณะที่ Edit Content และ Save เป็นคนละ right [S16]

Candidate custom rights profile `RequestorEditSave-v1`:

- Include VIEW (เปิด), DOCEDIT (แก้เนื้อหา), EDIT (บันทึก)
- Exclude EXPORT, OWNER, EXTRACT, PRINT, EDITRIGHTSDATA และสิทธิ์ที่ไม่จำเป็นต่อ fixture
- ไม่ใช้ built-in full Editor/Co-Author หรือ FullControl เพื่อทำให้ save สำเร็จโดยไม่ตรวจผล
- ทดสอบ editing + saving + reopening + denial สำหรับอีก user บน saved protected copy ด้วย

SDK constant names และการรองรับของ client ต้องตรวจ actual SDK APIs G1/G2 หาก limited profile ไม่ทำงาน ห้าม silently เพิ่ม broad rights ให้บันทึก compatibility gap แล้วตัดสินใจ requirement ก่อนขยาย profile

ไม่รับประกันการป้องกัน screenshot/การถ่ายทอดข้อมูลโดยผู้ที่ได้รับอนุญาต การมี download TTL ไม่ได้ revoke ไฟล์หรือ RMS offline license ที่ดาวน์โหลดแล้ว

## Identity binding

Authorization principal = validated `(tid, oid)`. Email/UPN เป็น RMS recipient identifier ที่ต้อง resolve จาก trusted source และ bind กลับ principal นั้น ห้ามใช้ request body/query string/filename หรือ unvalidated email claim เป็น authority

Demo baseline ใช้ operator-provisioned IdentityRecipientMapping สำหรับ A/B: tid, oid, canonical RMS address, mappingVersion, enabled, lastValidatedAtUtc และ evidence reference จาก G1/G2 validated tenant claims อาจใช้ช่วย resolve แต่ต้องตรวจ schema/semantics จริง ถ้า mapping ขาด ไม่ unique ถูกเปลี่ยน หรือผิด tenant ให้ fail closed

ไม่ normalize identity แบบทำให้สอง accounts รวมกันโดยไม่ตั้งใจ ไม่จำเป็นต้องขอ Graph permissions เพื่อ demo นี้ หาก directory lookup จำเป็นจริงให้เพิ่ม ADR และ least-privilege permission review

## Issuance algorithm

1. POST ขอ copy ด้วย document ID/version เท่านั้น ตรวจ Published/current-version+Eligible real intake evidence+ACL/tenant และ derive requester recipient
2. Snapshot source version/hash, owner metadata, label ID, rights profile/version, recipient mapping/version และ requester tid/oid
3. Read pinned source bytes; source update ไม่เปลี่ยน input ของ job เดิม หาก source Withdrawn/superseded/disabled หรือ ACL ถูกถอนระหว่างรอ ให้ fail/cancel release; pending new Draft ไม่เปลี่ยน published pointer
4. สร้าง output ใหม่ด้วย fresh protection descriptor สำหรับ recipient นี้เท่านั้น ตาม S14; labels แบบ UDP ไม่ใช้ admin-defined broad recipients เป็น fallback
5. Apply/commit ตามลำดับที่ documented สำหรับ installed SDK โดย handle custom-permission requirement อย่างชัดเจน
6. Independent inspection ต้องแสดง label ID, encrypted state, recipients/rights และ protection owner เท่าที่ API รองรับ เทียบ normalized expected set กับ observed set; ไม่เก็บเพียง request intent เป็น observation
7. ถ้าอ่าน recipients/rights ไม่ได้เพียงพอให้ gate unresolved และไม่ปล่อย live output จนมี proving approach ที่ยอมรับได้ ห้ามเพิ่ม SuperUser โดยอัตโนมัติ
8. Publish exact encrypted bytes เป็น immutable artifact version; SQL Ready binding รวม recipient fingerprint, rights fingerprint และ output hash
9. Download endpoint ให้เฉพาะ requester ที่ยัง authorized และ match recipient binding; B ขอ job A ผ่าน API ต้องไม่ได้

Idempotency fingerprint รวม tenant/requester/document/source version/rule/rights/mapping version; identical idempotency key ของ A/B ต้องไม่ชนกัน Cross-user cache reuse ห้ามแม้ source/label เหมือนกัน A download ครั้งใหม่อาจสร้าง job ใหม่แต่ retry key เดิมคืน job เดิมตาม contract

## Ownership design

- Business owner: person/group responsible for catalog entry in our app
- Requester: logged-in user requesting a specific copy
- MIP processor/protection owner: candidate service principal creating protection
- Recipient: requester address with limited rights

Do not set `DelegatedUserEmail=requester` merely to use login identity: that can make requester protection owner [S15]. Exact processor ownership/permission behavior is a live gate, not assumed from metadata

Source file ที่มี MIP protection อยู่แล้วไม่อยู่ใน MVP การเปลี่ยนสิทธิ์ของ protected source ต้องมี authorized decrypt/reprotect lifecycle และการทดสอบใหม่ ไม่ใช่แค่เรียก SetLabel ทับ

## Additional acceptance IDs

| ID | Case | Expected |
|---|---|---|
| U01 | A/B request same document/version | different artifacts/recipient evidence; same source hash and allowed label |
| U02 | A opens/edits/saves A, B opens/edits/saves B | succeeds and protection persists |
| U03 | B opens A and A opens B | rights-based denial in isolated client identities |
| U04 | Swap user identity in browser payload | rejected; server identity unchanged |
| U05 | Missing/stale/ambiguous recipient mapping | Failed, no release |
| U06 | Shared mutable descriptor/cache leaks A into B | test catches observed recipient mismatch, blocks Ready |
| U07 | Tamper recipient/rights evidence | release blocked; source expectation not substituted as observation |
| U08 | Business owner O1/O2 on files | preserved in UI/snapshot; not accidentally granted to every copy |
| U09 | Source republished/withdrawn during queued job | pinned bytes only; job release denied if version no longer current Published; pending draft alone has no effect |
| U10 | B downloads A via job ID or artifact reference | denied by application even before RMS |
| U11 | Saved copy A passed to B | still denied; allowed A control succeeds |
| U12 | Inspect rights | no broad domain/group grants, no unintended Export/Owner/Extract |

These tests add to docs/08. All live cases currently NOT RUN.

## Change limits after download

User-defined publishing rights are embedded in the document publishing license and are not the same as centrally managed template permissions [S14]. Removing a portal ACL or changing document owner does not automatically revoke a downloaded copy. Any later revoke/expiry requirement needs a separately tested RMS lifecycle design and must not be implied by application TTL.
