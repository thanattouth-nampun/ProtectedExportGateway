# 05 — API, configuration and data contracts v4.2

> ADR-013 update (19 October 2026): [Standalone owner-only MVP](standalone-owner-only-mvp.md) supersedes conflicting unprotected-library-source/service-owned-output assumptions below. External content connectors are deferred; Entra/MIP/Azure remain. Legacy library contracts below are implementation history until revised protected intake/owner mapping/G1/G2/G6 are proven. Report generation and all unaffected controls remain in scope.

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

Contract นี้เป็น proposed implementation ไม่มี endpoint ใดถูก deploy แล้ว ใช้ same-origin session cookies ทุก API ต้อง authenticate และ validate tenant; mutation requests ใช้ anti-forgery แม้เป็น JSON

React compiled assets are served by ASP.NET Core; domain APIs/models remain authoritative. Add OpenAPI-generated TypeScript DTOs and explicit session endpoints. Razor form antiforgery isn't automatically inherited; list/details share the same authorized API client/cache

## Endpoints

| Method/path | Access | Result |
|---|---|---|
| GET `/auth/login` | explicit browser login entry | server OIDC challenge, validated local return URL |
| GET `/auth/session` | authenticated | minimal authorized display claims; no access/refresh/MIP token |
| GET `/auth/csrf` | authenticated | no-store request antiforgery token bound to session/cookie |
| POST `/auth/logout` | session + CSRF | server cookie/OIDC signout; client clears authorized state |
| GET `/api/reports` | report roles | เฉพาะ definitions ที่ caller ใช้ได้; classification expectation ไม่ใช่ observed file state |
| GET `/api/reports/{id}/preview?period=...` | report-specific role | synthetic bounded preview; payroll role check เหมือน export |
| GET `/api/documents` | LibraryReader + document ACL, or business owner metadata scope | paginated allowed documents, owner/purpose/version; ไม่มี source URL |
| GET `/api/documents/{id}` | document ACL or business owner metadata scope | metadata only; owner role ไม่ให้สิทธิ์ copy โดยอัตโนมัติ |
| GET `/api/my/documents` | owner-scoped | drafts/versions/validation/publish status ของตน; ไม่แสดง draft ผู้อื่น |
| POST `/api/documents` | DocumentPublisher | title/purpose/allowlisted requester IDs; owner derive session; 201 Draft |
| PATCH `/api/documents/{id}` | DocumentPublisher+owner | If-Match; metadata/ACL เท่านั้น; no owner/label/rights override |
| POST `/api/documents/{id}/versions` | DocumentPublisher+owner | bounded multipart XLSX + CSRF/idempotency; 202 intake |
| GET `/api/documents/{id}/versions/{versionId}` | owner metadata scope | validation result; ไม่มี raw-original endpoint |
| POST `/api/documents/{id}/publish` | DocumentPublisher+owner | If-Match + versionId; Eligible evidence required |
| POST `/api/documents/{id}/withdraw` | DocumentPublisher+owner | If-Match; atomic withdrawal+audit, no file revocation claim |
| GET `/api/document-requester-options` | DocumentPublisher | bounded same-tenant allowlist IDs/aliases |
| POST `/api/documents/{id}/copies` | LibraryReader + CanRequestProtectedCopy ACL | 202 job; body มี sourceVersion เท่านั้น; requester/recipient derive จาก server |
| POST `/api/exports` | report-specific role | 202 + job location; idempotent replay 200 + existing job |
| GET `/api/exports` | authenticated | requester jobs; paginated, bounded page size |
| GET `/api/exports/{id}` | requester or auditor in tenant | status/evidence ที่ authorize แล้ว |
| GET `/api/exports/{id}/download` | requester AND current source-specific authorization | verified final XLSX; report role หรือ library ACL ตาม sourceKind |
| GET `/api/audit/exports` | ExportAuditor | paginated redacted metadata; ไม่ให้ download report |
| GET `/health/live` | local/operator policy | process liveness only, no tenant details |
| GET `/health/ready` | local/operator policy | DB/store/config/worker heartbeat; ไม่ทดสอบ encryption ทุก request |

Unknown/mismatched owner/tenant job ใช้ 404 เพื่อไม่เปิดเผย existence; สำหรับ known authorized job: 409 เมื่อ not ready/simulated, 410 เมื่อ expired, 503 เมื่อ storage/evidence integrity dependency ผิดพลาด Role ที่ไม่พอใน report create ใช้ 403

Owner mutations use 404 for foreign/mismatched owner resource, 403 for missing role, 412 stale If-Match, 413 oversize, 415 unsupported type, 409 invalid lifecycle. Create/upload/publish contracts in docs/12 supersede any former metadata-only owner scope. Reader catalog includes only current Published/Eligible documents authorized by ACL; owners see private drafts through owner-scoped endpoints.

API unauthenticated/expired session→structured 401, missing entitlement→403/404 per resource contract, never HTML login redirect in fetch. /api,/auth not intercepted by SPA fallback. Every mutation including logout uses CSRF; session/return URL/logout/cache semantics validated G7

## Request

```http
POST /api/exports
Content-Type: application/json
Idempotency-Key: <client-generated UUID>
X-CSRF-TOKEN: <anti-forgery value>

{
  "reportId": "payroll-detail",
  "parameters": { "period": "2026-09" }
}
```

Allowed report IDs และ periods มาจาก fixture definition ห้ามรับ labelId, userId, tenantId, path, recipients, SQL/filter expressions หรือ source URL ใช้ strict DTO/validation เพื่อ reject unknown fields ที่มีผลต่อ security ไม่ ignore แล้วทำให้คนเข้าใจว่า field มีผล Document request มีเพียง `{"sourceVersion":"<known-version>"}`; documentId มาจาก route ตรวจ ACL ก่อน resolve source; ไม่เปิด raw preview/download ของ original

Idempotency uniqueness: `(tenantId, requesterOid, key)`; canonical digest รวม sourceKind, report/document ID, parameters, pinned source/dataset version/hash, rule/rights/mapping versions และ requester recipient fingerprint; key reuse ต่าง payload → 409; replay เดิมคืน snapshot/job เดิมโดยไม่ remap จาก current config; ACL/source disabled ยังคงถูกปฏิเสธ เก็บ key ตลอด job metadata lifetime Different users same key never share artifacts

Download ของ ReportExport ตรวจ current report role; DocumentCopy ตรวจ current LibraryReader + document ACL + source enabled และ requester/recipient binding ใช้ `/api/exports/{id}/download` ร่วมกัน Auditor/business owner metadata access ไม่ให้สิทธิ์ download job คนอื่น

For DocumentCopy also require publicationStatus=Published AND job.sourceVersionId=CurrentPublishedVersionId AND bound real scan/validation evidence; Withdraw/superseded versions stop portal access even for existing Ready jobs. An uploaded new draft does not switch the pointer.

## Upload request example

```http
POST /api/documents/<document-id>/versions
Content-Type: multipart/form-data; boundary=<browser-generated-boundary>
Idempotency-Key: <client-generated-UUID>
X-CSRF-TOKEN: <anti-forgery-value>
If-Match: <document-revision>

<one bounded file part named file; no owner, recipients, label or blob URL>
```

One file ≤10 MiB/request ≤12 MiB. Stream to private quarantine, count/hash actual bytes, derive keys server-side. Multipart filename is sanitized display metadata, never a storage path. Intake idempotency uniqueness `(tid,oid,documentId,key)`; equal key/content returns intake/version, differing content rejects. Upload success=AwaitingValidation, not Published. Details and persisted digest/partial upload semantics in docs/12.

## Response

```json
{
  "jobId": "<opaque-id>",
  "mode": "LiveMip",
  "state": "Queued",
  "sourceKind": "ReportExport",
  "reportId": "payroll-detail",
  "ruleVersion": "demo-v1",
  "expected": { "labelAlias": "PayrollConfidential", "encrypted": true },
  "verification": null,
  "expiresAtUtc": null,
  "correlationId": "<trace-id>"
}
```

GET status หลัง Ready เพิ่ม observed label alias, actual protection state, hash, verifiedAtUtc และ expiry ส่วน `/download` ส่ง XLSX เท่านั้นและใช้ชื่อ server-generated อย่าให้ path หรือ bearer artifact URL ใน JSON

Errors ใช้ Problem Details + stable code + correlationId; raw exception/native logs อยู่ restricted diagnostics ถ้าเปิดไว้

| Code | Behavior |
|---|---|
| `REPORT_FORBIDDEN` | 403; ไม่สร้าง job |
| `REQUEST_INVALID` | 400; invalid period/field |
| `IDEMPOTENCY_CONFLICT` | 409 |
| `QUEUE_FULL` | 429; Retry-After |
| `LABEL_UNAVAILABLE` / `POLICY_STALE` | job Failed; ไม่มี fallback |
| `SDK_AUTH_FAILED` / `SDK_APPLY_FAILED` | job Failed หรือ retry เฉพาะ validated transient category |
| `VERIFICATION_FAILED` | job Failed; observed facts ยังเก็บไว้ |
| `ARTIFACT_INTEGRITY_FAILED` | หยุด download, mark Failed/quarantine, audit |
| `JOB_NOT_READY` / `SIMULATED_NO_RELEASE` | 409 |
| `ARTIFACT_EXPIRED` | 410 |
| `DEPENDENCY_UNAVAILABLE` | 503; ไม่มี release หาก audit/DB ล้มเหลว |
| `UPLOAD_TOO_LARGE` / `UPLOAD_TYPE_UNSUPPORTED` | 413/415; no Eligible source |
| `REVISION_CONFLICT` / `VERSION_NOT_ELIGIBLE` | 412/409; no lost update/publish |
| `SCAN_REJECTED` / `SCAN_INDETERMINATE` | async rejected/failed; no publishing |
| `CONTENT_VALIDATION_FAILED` | async rejected; safe reason only |
| `DOCUMENT_NOT_PUBLISHED` / `SOURCE_VERSION_SUPERSEDED` | reader request/download blocked; do not reveal private draft to unauthorized callers |

## Database entities

| Entity | Fields/constraints สำคัญ |
|---|---|
| ReportDefinition | Id, version, requiredRole, businessOwnerAlias, purpose, datasetVersion, allowedPeriods |
| RuleSnapshot | Id, reportId, ruleVersion, tenantId, processor identity alias, expectedLabelId, expectedProtection, configDigest, policyObservedAtUtc |
| ExportJob | Id, tenantId, requesterOid, reportId, canonical parameters, snapshotId, mode, state, created/ready/expires times, idempotencyKey, requestDigest, attemptCount, nextAttemptAt, leaseOwner/token/expiry, concurrencyVersion, artifactId, safeErrorCode |
| ExportAttempt | Id, jobId, attemptNumber, worker identity, fencingToken, start/end, SDK/runtime version, inputHash, outcome, safeErrorCode |
| Artifact | Id, jobId, attemptId, private storage key, SHA256, byteLength, sealedAtUtc, expiresAtUtc, deletion status |
| VerificationEvidence | Id, jobId, attemptId, artifactId, outputHash, observedLabelId, observedProtection tri-state, property semantics version, inspectedAtUtc, result |
| AuditEvent | Id, jobId, tenantId, actorKind/alias or oid, eventType, timestampUtc, correlationId, mode, typed redacted details |
| ClientAccessEvidence | Id, runId, artifactHash, identity alias, expected/observed access, client version, timestampUtc, controlled evidence reference |
| Document | Id, tenantId, title/purpose, businessOwnerTid/Oid, ownerAlias, publicationStatus, currentPublishedVersionId, revision, aclRevision, enabled kill-switch |
| DocumentVersion | Id, documentId, uploadedByTid/Oid, intakeId, displayFilename, intakeState, quarantine/source bindings (container/blob/version/ETag/hash/size), mediaType, uploaded/validated times, safe rejection reason |
| DocumentAcl | documentId, principalTid/Oid or reviewed app-role mapping, CanRequestProtectedCopy; no client-managed ACL |
| IdentityRecipientMapping | tenantId, requesterOid, canonicalRmsAddress, version, enabled, lastValidatedAtUtc, validationEvidence |
| RightsProfile | Id/version, normalized allowed/denied rights; RequestorEditSave-v1 candidate |
| IntakeJob | document/version, uploader, mode, stage, lease/fencing/attempt/timeout, idempotency/request digest and verified source binding |
| ScanEvidence | actual input hash, provider/platform/engine/intelligence version/time, verdict, runId, timestamps, mode; no intended-only Clean |
| ValidationEvidence | exact input hash, parser/profile/build/limits and OOXML/unprotected-content results |
| PublicationEvent | document revision/version, actor, publish/withdraw/ACL-change, time/correlation; atomic with current pointer/ACL |

Extend ExportJob with sourceKind/reportId-or-documentVersionId, businessOwner snapshot, recipient mapping snapshot/fingerprint, rightsProfileVersion and sourceHash. RuleSnapshot carries protection profile TemplateBased/LabelOnly/UserDefined. Artifact carries Blob versionId/ETag/container/key; VerificationEvidence carries independently observed recipients/rights/protection owner and their semantics version. SQL Server provider/Azure SQL migrations are baseline; SDK observations are nullable/unknown until actually read, never filled from expected values.

Foreign keys required; unique attempt numbers per job; one active artifact binding per Ready job; indexes on queue state/nextAttempt and requester/createdAt; transactions ตรวจ fencing/version ก่อน commit

ไม่เก็บเงินเดือน/ชื่อจาก dataset ใน audit DB ค่า preview/report content สร้างจาก synthetic fixture; AuditEvent เป็น application journal ไม่ใช่ Microsoft Purview native audit และไม่ใช่ tamper-proof ledger

## Configuration example — placeholders only

```json
{
  "Gateway": {
    "Mode": "Simulated",
    "HostingProfile": "Local",
    "TenantId": "<tenant-id>",
    "DataRoot": "<absolute-private-data-root>",
    "ArtifactTtlMinutes": 60,
    "MaxQueueLength": 20,
    "MaxArtifactBytes": 10485760,
    "PolicyMaxAgeMinutes": 15
  },
  "Cloud": {
    "SqlServer": "<azure-sql-host>",
    "Database": "<database-name>",
    "BlobServiceUri": "<private-blob-account-endpoint>",
    "SourceContainer": "sources",
    "ExportContainer": "exports",
    "QuarantineContainer": "quarantine",
    "KeyVaultUri": "<key-vault-uri>"
  },
  "DocumentProtection": {
    "UserDefinedLabelId": "<udp-label-id>",
    "RightsProfile": "RequestorEditSave-v1",
    "RecipientMappingSource": "ServerManaged",
    "ProtectionOwnerMode": "ServicePrincipal"
  },
  "DocumentIntake": {
    "MaxFileBytes": 10485760,
    "MaxRequestBytes": 12582912,
    "ScannerProvider": "WindowsDefenderCandidate",
    "ScannerHelperReference": "<trusted-local-helper-reference>",
    "ScannerSignatureMaxAgeHours": 24,
    "ValidationProfile": "BoundedUnprotectedXlsx-v1",
    "QuarantineRetentionHours": 24,
    "AllowLivePublishFromSimulatedScan": false
  },
  "WebIdentity": {
    "ClientId": "<web-app-id>",
    "CertificateReference": "<secret-store-reference>"
  },
  "ProcessorIdentity": {
    "ClientId": "<processor-app-id>",
    "CertificateReference": "<secret-store-reference>"
  },
  "ReportLabelMappings": {
    "headcount-summary": {
      "LabelId": "<label-only-id>",
      "ExpectedProtection": false,
      "RuleVersion": "demo-v1"
    },
    "payroll-detail": {
      "LabelId": "<encrypted-label-id>",
      "ExpectedProtection": true,
      "RuleVersion": "demo-v1"
    }
  }
}
```

เลือก LiveMip ต้อง fail startup/readiness หากยังมี placeholder/config invalid; label verification gate ยังต้องทำจริงใน worker Simulated mode ห้ามสลับ job เดิมเป็น live หรือใช้ evidence ของ fake adapter ปะปน

Local DataRoot เป็น staging/filesystem adapter เท่านั้น ใน HostingProfile=Azure authoritative sources/artifacts ต้องเป็น private Blob และ DB เป็น Azure SQL; web local disk ไม่ใช่ shared final store ทั้ง Cloud options/credentials validate ตาม hosting profile ไม่บังคับ secret จริงเพื่อรัน fake local tests

Live publishing additionally requires G6-backed scanner/helper capability and actual evidence; placeholder/unavailable scanner blocks publish even if SDK works. Boolean `AllowLivePublishFromSimulatedScan` is always false and cannot be enabled as an operator bypass. Local fake intake can exercise states but not enter live reader catalog.
