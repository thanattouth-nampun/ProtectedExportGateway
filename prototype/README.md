> Current decision v4.2 / ADR-012: the product uses an HTML list/detail workspace; 3D is removed. This file describes the historical prototype only. Use docs/13–15 for current requirements.

# Folio legacy UX prototype v4 — flow reference for v4.1

ต้นแบบ local สำหรับตัดสินใจ UX/UI เท่านั้น ชื่อ Folio เป็น working name

Current architecture v4.1 adopted React+real R3F/Three/Drei per [assessment](../docs/15-long-term-stack-assessment.md). This existing prototype remains buildless HTML/CSS/SVG: **not** actual 3D or new frontend implementation and cannot pass G7. Use its metadata/Owner/status flows as reference, not its static illustration as final 3D scope

เปิด `dist/index.html` ใน browser ได้โดยตรง หรือ serve โฟลเดอร์ `dist/` ด้วย static server ของทีม ไม่ต้อง npm install/build ไม่มี remote dependencies, external fonts, API calls หรือ cloud resource

## สิ่งที่ลองกดได้

- ค้นหา/กรอง Owner ใน library และเปิดรายละเอียด
- ขอสำเนาจำลอง กดขั้นตอนถัดไปเพื่อดู status/rights intent; ไม่มี file download
- เลือกบทบาทจำลอง A/B เพื่อตรวจหน้าประวัติคนละชุด
- Owner O1/O2 เพิ่มเอกสารด้วย wizard ไฟล์/รายละเอียด→allowlist→review; ใช้ sample file เพียงชื่อ/ขนาด ไม่มีการส่ง/อ่านเนื้อหาไฟล์
- จำลอง validation outcome, publish/withdraw, versions และแก้ metadata/ACL
- Reports, history, auditor metadata และหน้า sign-in ตัวอย่าง
- Narrow screen navigation and document cards

ข้อมูลเริ่มต้นทั้งหมดเป็น fixtures และการเปลี่ยนแปลงอยู่ใน memory; reload เพื่อเริ่มใหม่ ไม่มี authentication/AV/MIP/Azure/DB จริง สถานะ published/ready/denied ที่แสดงเป็น design intent เท่านั้น ไม่มีการทำให้ source จริง eligible/published หรือพิสูจน์สิทธิ์เปิดไฟล์

Production ต้องไม่มี persona selector/simulation-next buttons และต้องใช้ backend authorization/state/evidence ตาม docs/01–12 ไม่ใช้ JavaScript ของ prototype เป็น security boundary

See [UX specification](../docs/13-ux-ui-design.md) and [component inventory](../docs/14-component-inventory.md). Original HTML/CSS/JS/illustration created for this prototype, no 21st source installed or copied

## Verification performed

- JavaScript syntax check via Node: PASS
- Browser: library/detail, A request stages/rights-intent, B empty own-history, Owner wizard/sample file→validation fixture→publish confirmation→withdraw confirmation and action: inspected successfully
- Browser viewport 390px: card layout and menu inspected; measured no page-width overflow in observed state
- Not a full accessibility audit: screen-reader, 200% zoom, contrast of every pair, native file selection/drag-drop, concurrent versions/race conditions and all error paths still require testing
- Live Entra/MIP/scanner/Azure gates: NOT RUN
