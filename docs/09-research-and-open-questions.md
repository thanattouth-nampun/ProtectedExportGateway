# 09 — Research register, assumptions and open questions v4.2

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

ตรวจเอกสารสาธารณะ: 16 ตุลาคม 2026 (Asia/Bangkok). นี่คือ document research ไม่ใช่ tenant validation เอกสาร/preview/status เปลี่ยนได้ ให้ตรวจใหม่ก่อน implementation หรือ public release

## Primary sources

| ID | Source | Fact used / limitation |
|---|---|---|
| S01 | [MIP API concepts](https://learn.microsoft.com/en-us/information-protection/develop/concept-apis-use-cases) | File SDK รวม policy/protection; vendor อธิบาย LOB export use case ไม่ใช่หลักฐานว่าลูกค้าของเราต้องการซื้อ |
| S02 | [Required API permissions](https://learn.microsoft.com/en-us/information-protection/develop/concept-api-permissions) | แยก app/delegated rights; Content.Writer สร้าง protected content ใน service principal context; ต้องทดสอบ actual engine/policy setup |
| S03 | [Setup and configuration](https://learn.microsoft.com/en-us/information-protection/develop/setup-configure-mip) | Tenant/client/app prerequisites, SDK logging consent และ IPIA ก่อน public release; internal-only app มีข้อยกเว้นตามเอกสาร |
| S04 | [.NET wrapper overview](https://learn.microsoft.com/en-us/information-protection/develop/concept-summary-csharp) | Wrapper ใช้ native C++ libraries และประกาศ .NET Standard consuming runtime support; exact deployment combo ต้องพิสูจน์ |
| S05 | [C# set/get label quickstart](https://learn.microsoft.com/en-us/information-protection/develop/quick-file-set-get-label-csharp) | SetLabel, commit output แล้วใช้ handler ใหม่อ่าน label; sample ไม่ใช่ production web architecture |
| S06 | [Supported file types](https://learn.microsoft.com/en-us/information-protection/develop/concept-supported-filetypes) | XLSX อยู่ใน supported formats; native/generic protection มี semantics ต่างกัน |
| S07 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | เลือก .NET 10 LTS candidate ตาม support lifecycle ปัจจุบัน; ไม่ใช้ sample runtime เก่าเป็นค่าเริ่มต้น |
| S08 | [ClosedXML documentation](https://docs.closedxml.io/en/latest/) | สร้าง XLSX บน server โดยไม่ใช้ Excel application; ตรวจ actual package/license ตอน pin |
| S09 | [MIP FAQ](https://learn.microsoft.com/en-us/information-protection/develop/faq) | SDK ไม่เขียน watermark/header/footer ลงไฟล์โดยตรง และ output labeling เป็นไฟล์สำเนา; ไม่แก้ read/write ไฟล์เดียวกันพร้อมกัน |
| S10 | [Purview Information Protection](https://learn.microsoft.com/en-us/purview/information-protection) | Licensing ขึ้นกับ feature/scenario; ไม่มีการยืนยัน tenant entitlement ในงานนี้ |
| S11 | [Unified Catalog](https://learn.microsoft.com/en-us/purview/unified-catalog) | Governance รองรับไฟล์ใน data products แต่ integration อยู่นอก MVP |
| S12 | [Data Map sensitivity labels](https://learn.microsoft.com/en-us/purview/data-map-sensitivity-labels) | Catalog label metadata ต่างจาก label/protection บน physical file; feature ระบุ preview ณ วันที่อ่าน |
| S13 | [Graph assignSensitivityLabel](https://learn.microsoft.com/en-us/graph/api/driveitem-assignsensitivitylabel?view=graph-rest-1.0) | ทางเลือกเมื่อไฟล์อยู่ SharePoint/OneDrive; protected/metering requirements ต้องตรวจ ไม่มี Graph dependency ใน MVP นี้ |
| S14 | [MIP user-defined permissions](https://learn.microsoft.com/en-us/information-protection/develop/concept-user-defined-permissions) | แอปกำหนด recipients/rights ต่อ protected document ได้; publishing rights ฝังในเอกสาร ไม่ใช่แก้ global label recipients ทุก download |
| S15 | [MIP delegation](https://learn.microsoft.com/en-us/information-protection/develop/concept-delegation) | Delegated protection identity อาจกลายเป็น protection owner; ไม่ delegate requester โดยอัตโนมัติ |
| S16 | [Rights Management usage rights](https://learn.microsoft.com/en-us/purview/rights-management-usage-rights) | Edit Content/DOCEDIT และ Save/EDIT แยกกัน; built-in Editor มีสิทธิ์กว้างกว่า edit/save และ client behavior ต้องทดสอบ |
| S17 | [App Service overview](https://learn.microsoft.com/en-us/azure/app-service/overview) | managed hosting สำหรับ web; ไม่ยืนยัน native MIP worker ใน sandbox ของแอปเรา |
| S18 | [App Service VNet integration](https://learn.microsoft.com/en-us/azure/app-service/overview-vnet-integration) | ใช้ outbound private connectivity; inbound privacy เป็นอีก configuration |
| S19 | [Authorize Blob access with Entra ID](https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-access-azure-active-directory) | data-plane roles/MI สำหรับ private Blob; control-plane Contributor อย่างเดียวไม่แทนสิทธิ์อ่าน bytes |
| S20 | [Azure SQL managed identity](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-azure-ad-user-assigned-managed-identity?view=azuresql) | ใช้ Entra identity ต่อ SQL; ต้อง bootstrap DB authorization และ network ให้ถูกต้อง |
| S21 | [Key Vault certificate access control](https://learn.microsoft.com/en-us/azure/key-vault/certificates/certificate-access-control) | public certificate metadata กับ private-key/secret access ต่างกัน; scoped RBAC ต้องทดสอบ |
| S22 | [Certificates with Microsoft.Identity.Web](https://learn.microsoft.com/en-us/entra/msidweb/authentication/certificates) | credential retrieval จาก Key Vault ต้องเข้าถึงส่วน certificate/private key ที่เกี่ยวข้อง; ไม่ใส่ PFX ใน repo |
| S23 | [Cost alerts](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/cost-mgt-alerts-monitor-usage-spending) | budgets แจ้งเตือน ไม่ใช่ hard spending cap หรือ shutdown อัตโนมัติ |
| S24 | [Blob versioning](https://learn.microsoft.com/en-us/azure/storage/blobs/versioning-overview) | versions มี IDs แยกกันและใช้พื้นที่/ค่าใช้จ่าย; validate exact account configuration |
| S25 | [Conditional Blob headers](https://learn.microsoft.com/en-us/rest/api/storageservices/specifying-conditional-headers-for-blob-service-operations) | conditional reads/writes ด้วย ETag; SHA256 integrity check ของแอปยังจำเป็น |
| S26 | [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/) | ใช้ทำ estimate หลังเลือก region/offer/SKUs/hours จริง; ยังไม่มีราคาในเอกสารนี้ |
| S27 | [ASP.NET Core file uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0) | bounded upload, untrusted filename/content, private non-executable storage and file checking; configuration ต้อง test บน hosting จริง |
| S28 | [MpCmdRun command-line documentation](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus) | candidate scanner invocation/permissions/result semantics; generic exit0 can mean clean or successful remediation จึงไม่ใช้เป็น sole Clean proof |
| S29 | [Defender Antivirus on Windows Server](https://learn.microsoft.com/en-us/defender-endpoint/microsoft-defender-antivirus-windows-server-configure) | verify actual installation/mode/platform/update readiness; ไม่อนุมาน MDE onboarding หรือ entitlement |
| S30 | [Defender for Storage malware scanning](https://learn.microsoft.com/en-us/azure/defender-for-cloud/introduction-malware-scanning) | optional cloud scanner; tags not tamper-resistant sole authority, encrypted client-side content not automatically scannable; ยังไม่เลือก/enable เป็น baseline |
| S31 | [21st.dev catalog](https://21st.dev/community/components), [File Upload](https://21st.dev/@extend-hq/components/file-upload-2), [Staged Progress Bar](https://21st.dev/@dhxwhy/components/staged-progress-bar) | React/Tailwind catalog and shortlist; File Upload page lists MIT, other exact licenses/dependencies require review before reuse; no code installed/copied |
| S32 | [WCAG 2.2](https://www.w3.org/TR/WCAG22/) | accessibility design/test target for contrast, focus, keyboard and responsive controls; no conformance audit performed |
| S33 | [React creation guidance](https://react.dev/learn/creating-a-react-app), [build-tool tradeoffs](https://react.dev/learn/build-a-react-app-from-scratch) | React framework recommendation and explicit routing/query choices for constrained SPA; Vite isn't complete app framework |
| S34 | [R3F introduction](https://r3f.docs.pmnd.rs/getting-started/introduction), [performance](https://r3f.docs.pmnd.rs/advanced/scaling-performance), [Drei controls](https://drei.docs.pmnd.rs/controls/introduction) | actual React/Three renderer, compatible peer versions/lifecycle/performance techniques; app performance NOT RUN |
| S35 | [ASP.NET antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [cookie auth](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) | SPA cookie session requires explicit CSRF/auth behavior; no claim sample settings automatically satisfy BFF |
| S36 | [Microsoft React/.NET tutorial](https://learn.microsoft.com/en-us/visualstudio/javascript/tutorial-asp-net-core-with-react?view=visualstudio), [Vite deploy](https://vite.dev/guide/static-deploy.html) | separate frontend project can build static assets hosted by .NET; no second Node server required by chosen design |
| S37 | [WebGL best practices](https://developer.mozilla.org/en-US/docs/Web/API/WebGL_API/WebGL_best_practices) | client GPU/resource/context constraints require actual browser/device tests |

วิดีโอที่ผู้ใช้ให้: [Getting started with File SDK](https://www.youtube.com/watch?v=gvfN4yeXZnA&t=141s). เปิดตรวจเนื้อหาไม่ได้ในรอบวิจัย จึงไม่ใช้เป็นหลักฐานของ capability หรือ configuration

## Facts vs design vs untested claims

| Statement | Type | Evidence/status |
|---|---|---|
| File SDK เหมาะกับ label-on-export | vendor-documented capability | S01 |
| .NET wrapper ต้องพิจารณา native libraries | documented fact | S04 |
| Gate ต้อง inspect output ใหม่ | design backed by sample pattern | S05; implementation ยังไม่มี |
| App-only flow ด้วย certificate เป็น baseline ที่เหมาะ | design candidate | S02 + operational choice; G1 NOT RUN |
| `.NET 10 + installed MIP version + Windows host` ทำงานร่วมกัน | untested | G1 NOT RUN |
| SDK adapter ตรวจ actual protection ได้ครบตาม invariant | untested | ต้องระบุ installed API/property semantics ใน G1 |
| HR users จะได้ประโยชน์จาก export controls | business hypothesis | ยังไม่มี interview/pilot |
| Protected file ปฏิเสธ denied user ได้ | intended configuration outcome | G2 NOT RUN |
| System audit = Microsoft Purview audit | false assumption | สองอย่างนี้แยกกัน; ห้ามอ้างรวม |
| Catalog label หมายถึง physical encryption | false assumption | S12 ระบุ catalog metadata |
| Gateway ควบคุมการนำข้อมูลออกทุกช่องทาง | unsupported claim | อยู่นอกขอบเขต |
| A/B ได้สำเนา rights ต่างกันจาก source เดียว | user-approved requirement + documented building blocks | S14; end-to-end NOT RUN |
| Recipient เป็น Editor แล้วไม่มี Export/Copy | unsafe assumption | S16; ต้อง custom limited edit/save profile และ client tests |
| Service-owned UDP copy inspect recipients/rights ได้ครบ | untested | G1 ต้องระบุ API/permission/semantics จริง |
| Business owner ต้องเป็น RMS owner/recipient | not implied | baseline metadata-only; ownership policy ต้องระบุชัด |
| Azure App Service web + Windows VM worker เป็น deployment ที่ผ่านแล้ว | false assumption | engineering proposal; G5 NOT RUN |
| Managed identity ของ Azure ใช้แทน MIP processor credential ได้ทันที | unverified | baseline แยก MIP app certificate; อย่าอนุมานว่า Azure resource RBAC ให้ MIP rights |
| Owner upload/version/publish/withdraw | user-approved v3 requirement | docs/12; implementation/lifecycle tests NOT RUN |
| Existing Windows VM has usable AV helper/clean verdict semantics | untested | G6 NOT RUN; source documentation ไม่แทน actual engine test |
| Scanner return0 proves clean input | unsafe assumption | S28; possible remediation ต้อง reject/verify facts |
| Scan clean means guaranteed malware-free | unsupported claim | scanner observation only; no guarantee/compliance claim |
| Withdraw portal revokes copies already downloaded | false assumption | separate MIP/offline-license lifecycle; out of current requirement |
| React is inherently more secure or faster than Razor | unsupported generalization | stack selected for clarified interactive product needs, not universal superiority |
| React/3D necessarily needs a Node server/GPU VM on Azure | false assumption for chosen architecture | static frontend on .NET same-origin host; client GPU; additional data/bundle costs still measured |
| Current prototype is actual 3D/React implementation | false | legacy buildless HTML/CSS/SVG reference only; G7 NOT RUN |

## Open questions and owners

| ID | Question | Owner | Blocks |
|---|---|---|---|
| Q01 | Tenant และ license ใดใช้ทดสอบ และใครบ้างได้ entitlement ตาม scenario? | environment owner | G0/live |
| Q02 | App-only engine ต้องตั้ง identity/policy context อย่างไรกับ SDK stable ที่เลือก? | developer + tenant admin | G1 |
| Q03 | Certificate, consent และ label publication ที่จำเป็นพร้อมไหม? | tenant admin | G1 |
| Q04 | Label-only/encrypted label IDs, scopes, recipients และ owner ที่คาดหวังคืออะไร? | information protection admin | G1/G2 |
| Q05 | SDK property ใดตรวจ actual protection โดยไม่ต้อง broad decryption rights? | developer | Ready implementation |
| Q06 | Policy refresh API/cache lifecycle ของ exact version รองรับ freshness strategy อย่างไร? | developer | G1/G3 |
| Q07 | Test users/isolated Excel client และ rights-based denial evidence พร้อมไหม? | environment owner | G2 |
| Q08 | Host OS/native prerequisites และ .NET 10 combo ผ่านไหม? | developer | G1 |
| Q09 | Demo เป็น internal-only หรือแอปจะ public/distributed to customers? | project owner | deployment/IPIA review |
| Q10 | ระบบธุรกิจจริงใดเหมาะเป็น pilot และรู้ classification จาก context ได้มั่นใจเพียงใด? | project owner + report owner | pilot value |
| Q11 | Artifact TTL/audit retention เป้าหมายนี้ตรงความต้องการจริงหรือไม่? | project owner | pilot; demo defaults ใช้ได้ |
| Q12 | UDP label ID/scope/publication และ least edit/save profile บน client ที่เลือกใช้ได้หรือไม่? | protection admin + developer | G1/G2 |
| Q13 | A/B tid/oid-to-RMS-address mapping และ source owners O1/O2/ACL ใดใช้ demo? | tenant/app owner | per-user copy gate |
| Q14 | Independent readback API ใดพิสูจน์ recipients/rights/owner ด้วย service flow โดยไม่ SuperUser? | developer | G1 release invariant |
| Q15 | Business owner ต้อง retain RMS access ในทุก copy หรือไม่? | project owner | baseline metadata-only ยังใช้ได้; extra grant ต้องเปลี่ยน requirement |
| Q16 | Subscription/region/SKUs/cost ceiling/demo audience/private admin access เลือกอะไร? | Azure environment owner | provisioning/G5 |
| Q17 | Key Vault credential loading, SQL grants และ private endpoints/MI scopes ทำงานบน hosts จริงหรือไม่? | developer + Azure operator | G5 |
| Q18 | VM runtime/native dependencies และ supervised timeout/recovery ใช้ได้จริงหรือไม่? | developer + Azure operator | G5 |
| Q19 | O1/O2 Publisher roles, same-tenant ACL allowlist และ metadata defaults ใช้อะไร? | tenant/app owner | live intake |
| Q20 | AV engine/mode/signatures/licensing/helper privileges ที่มีจริงรองรับ reliable non-remediating scan evidence หรือไม่? | environment/security owner + developer | G6/live publish |
| Q21 | bounded XLSX parser/profile limits รองรับ demo workbook จริงโดยไม่ execute/รับ active content หรือไม่? | developer | G6 |
| Q22 | quarantine/custom source-promotion RBAC, retained source versions และ 24h cleanup targets เหมาะกับ environment หรือไม่? | Azure operator + app owner | G5/G6 |
| Q23 | Compatible React/TS/Vite/router/query/forms versions and component/font licenses? | developer | G7 |
| Q24 | Representative browser/device/network profiles for HTML workspace load and reflow? | project owner + developer | G7 |
| Q25 | Same-origin session/CSRF/API routes/key persistence/identity-cache behavior proven on actual release? | developer + Azure operator | G7 |

## Business discovery for pilot

หลัง MVP ไม่ต้องถามความต้องการทุกเรื่อง ให้ถาม HR/IT/report owner ที่เกี่ยวข้อง 3–5 คนเป็น design target: ปัจจุบัน export อะไร, ใครเป็นผู้กำหนด label, มีช่องทาง bypass ใด, เคยต้องแก้ไฟล์ที่ไม่มี label หรือไม่, ใครรับผิดชอบ failure และการเข้ารหัสกระทบงานใด

บันทึก observations กับ quotes ที่ได้รับอนุญาต แยกจากข้อสรุปของผู้พัฒนา ถ้าระบบเดิมทำ label-on-export ได้ครบแล้ว ให้ประเมิน integration gap ก่อนลงทุนขยาย gateway

## Phase 2 candidates — outside current v4.1 scope

1. เชื่อม report export จาก LOB จริงหนึ่งระบบพร้อม contract/authorization review
2. เพิ่ม PDF หลังพิสูจน์ reader/protection และไฟล์ที่ signed/password-protected ตาม supported behavior
3. Approval ก่อน release เมื่อมี requirement จาก owner จริง
4. เชื่อม governance catalog เพื่ออ้างอิง owner/purpose/assets โดยไม่อ้างว่า metadata ทำ encryption
5. เพิ่ม horizontal scale/multi-worker หรือเปลี่ยน native worker ไป PaaS หลังวัด workload/compatibility; Azure SQL/Blob/split web-worker เป็น v2 baseline อยู่แล้ว

## Change log

- 2026-10-16: ผู้ใช้เลือก Protected Export Gateway; จัดทำ design baseline, repository handoff และ feasibility-first plan ยังไม่มี tenant access, implementation หรือ runtime validation
- 2026-10-16 v2: ผู้ใช้เพิ่ม Document Library, owner ต่างกัน, per-user edit/save copies ที่คนอื่นเปิดไม่ได้ และ Azure resources/hosting ปรับ requirements/API/security/tasks/tests/prompt ทั้งชุด พร้อม docs/10 และ docs/11 ยังไม่ได้สร้าง resources หรือยืนยัน live behavior
- 2026-10-16 v3: ผู้ใช้เพิ่ม Owner/Publisher upload, metadata/ACL/version management, quarantine+malware/OOXML validation และ publish/withdraw เพิ่ม docs/12/G6 และปรับ API/RBAC/storage/tasks/tests/prompt ทุกส่วน ไม่มี implementation/scanner execution/resource provisioning ในรอบนี้
- 2026-10-16 v4: เพิ่ม UX/UI design, 21st.dev sourcing/compatibility review และ original local clickable prototype inspired by user's isometric references; retains v3 backend/Azure scope; no catalog code copying, real auth/upload/scanning/protection/deployment or accessibility certification
- 2026-10-16 v4.1: user clarified actual interactive 3D and authorized assessed stack change; adopt React/TS/Vite/R3F with same-origin .NET BFF and existing MIP worker. Add docs/15/ADR-011/G7 and update conflicting Razor/static-only instructions. Legacy prototype retained and marked; no new React/3D implementation/benchmark or Azure provisioned

- 2026-10-19 v4.2 / ADR-012: user removed 3D as unsuitable. Retain React/.NET and all security/native/intake/Azure controls; HTML list/detail workspace replaces renderer requirements. Prior renderer research/evidence remains historical.
