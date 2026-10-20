# ADR-014 — Self-service workspace for tenant Members

Product decision approved by the human on 20 October 2026: every internal Member of the configured tenant has a private workspace without manual Publisher/Reader enrollment. This supersedes only conflicting Publisher/pre-enrolled-owner requirements for empty drafts in ADR-013 and earlier documents. ADR-013 source ownership/protection/intake/release requirements, reports and auditor boundaries remain. No external connector, general file-storage product, collaborative editor, anonymous upload or multi-tenancy is added.

## Behavior and checkpoint

- **My documents:** prepare private drafts and ACLs; future verified uploads/versions/publish/withdraw remain gated. New drafts have no automatic recipients.
- **Shared with me:** only published/ACL-authorized documents. Current API remains unavailable; no fixture document is published.
- **My downloads:** requester-scoped history; actual issuance/download remains gated.

Internal means Entra **Member account classification**, proven by a single signed ID-token acct=0 plus valid configured tid and nonempty oid. Guest acct=1, missing/duplicate/unknown acct, foreign tenant and anonymous identities cannot manage drafts. Email suffix or Publisher role cannot establish membership. This does not claim Member classification proves employment or excludes externally authenticated users converted to Members; administrators control account classification.

The BFF returns `canManageOwnDocuments` derived from its validated session, not a fabricated Publisher role. APIs enforce the same Member policy independently of React. SQL authorizes the trusted member actor, checks tenant/oid and scopes list/find/update/audit to the session owner. A Reader Member can own a draft; no membership grants access to another member's private draft. Cross-owner find/update/audit returns 404. Rowversion concurrency, transaction/audit and source-bearing mutation blocks remain.

New Members do not need a reviewed RMS-directory entry to create empty drafts. The directory still bounds **recipient choices** (currently Owner/A; max 50 selected). An all-tenant people picker/directory sync is not implemented here. Trusted uploader-to-RMS-address mapping and independently observed RMS ownership remain necessary for future source intake. Session display/email claims do not prove ownership.

React shows My documents for the server member capability and Shared with me for Members/legacy Readers. Changes to the capability participate in the authorized QueryClient identity key, clearing selection/forms/cache. Report/payroll/auditor permissions are not granted by membership. No schema migration/dependency upgrade; legacy unprotected publication contracts are not made eligible.

## Approved tenant rollout

The human subsequently approved these exact two changes. The setup script completed VERIFIED and the new local host was activated; see [actual rollout evidence](evidence/2026-10-20-member-workspace-live.md). Fresh A/B session and private-draft acceptance subsequently passed, including B with roles=[] and no app assignment. The prestate below is retained for rollback; these actual checks are separate from the 183 automated tests.

Read-only inspected prestate and exact payloads: `.runtime/member-workspace/change-plan.json`.

| Item | Exact target/change |
|---|---|
| Tenant | baht.net / 9fbc4f5d-56ec-4074-82f0-69d9a86a7c06 |
| Web app | ProtectedExportGateway-Web-Local / a834c776-387c-43aa-8d55-3ab62a08acf6 |
| Application object | a490d041-acd3-43a3-89c2-94820c6dabae |
| App patch | Add only optional ID-token claim acct; observed optionalClaims=null |
| Service principal | 3cd6f447-4747-403a-8741-a034f3870e9c |
| SP patch | appRoleAssignmentRequired true -> false |
| Effect | Users of this tenant can attempt sign-in without manual role assignments; own-document access still requires verified Member status |
| Preserved | Single-tenant authority, roles, existing openid/profile consent, certificates, MIP app, source/ACL/release controls |
| New API permissions | None |
| Additional Azure resource estimate | THB 0; no provisioning/deployment; existing subscription/license costs excluded |

Sessions issued before the change have no acct evidence; the updated API fails closed until a fresh sign-in produces it. Activation followed the approved claim setup and readback. No fabricated membership claim or browser token storage is used.

Rollback: stop the new host; restore exact app optionalClaims=null and SP assignmentRequired=true from the snapshot; use the prior local artifact if needed. Preserve all user drafts/audit, roles, consent, MIP identity and certificates. Changed claims require fresh sign-in; toggling assignment-required does not instantly revoke existing sessions. Cloud deployment is separately unapproved.

## Verification and next work

Local tests use synthetic signed-in principals and isolated real LocalDB; intercepted browser tests are not Entra evidence. The earlier live A Publisher-denial result is historical evidence for the former policy. No live self-service/all-member claim is made until rollout and fresh login.

Completed live checkpoint: A and role-free B created own private drafts; B reloaded/updated its own draft; A could not read Owner's known draft/audit and B could not read A's. SQL readback confirmed separate ownership/audits and original Owner unchanged. B's session roles=[] and post-login direct assignment count=0 prove a non-enrolled Member path. Guest/unknown claims and symmetric cross-owner mutation remain synthetic/SQL tests; not every live denial direction was manually exercised.

Continue T23 quarantine and verified uploader/RMS-owner binding, G6 content inspection/scanner/OOXML validation, then publish/withdraw and per-user protected release with hash/exact Blob-version evidence. No Upload/Publish/Ready/download gate passes from metadata workspaces alone.

Sources: [acct claim reference](https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims-reference), [optional ID-token claims](https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims).
