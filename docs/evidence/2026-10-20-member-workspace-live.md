# Approved Member workspace local rollout — 20 October 2026

This follows the separately documented 183-test local checkpoint and the human's explicit approval of the two exact tenant changes in ADR-014. It is not a cloud deployment or protected-intake acceptance.

## Actual tenant changes

`scripts/setup-member-workspace.ps1 -ApprovedSetup` passed parser validation and completed VERIFIED. Before mutation, target-bound Graph reads confirmed the exact single-tenant web app and SP, optionalClaims=null and assignment-required=true. The script journaled the prestate and each attempted/acknowledged operation.

- App ProtectedExportGateway-Web-Local / a834c776-387c-43aa-8d55-3ab62a08acf6: only optional ID-token acct added, with no extra properties/source/essential flag or access/SAML claims.
- SP 3cd6f447-4747-403a-8741-a034f3870e9c: appRoleAssignmentRequired=false.
- Independent readbacks verified the exact changes and unchanged role/default assignments, consent grants, sign-in audience, API permissions, web redirects/settings, app-role definitions and credentials. No MIP identity changes, new users/licenses, additional API grants, Azure resources or deployment.
- Private `.runtime/member-workspace/execution-journal.json` records target/prestate, approval, operation stages and preservation flags; before/after snapshots remain ignored. No token/private key is included in tracked evidence.

Additional Azure resource estimate remains THB 0, excluding existing licenses/subscription. Rollback: stop the new host; restore only this app optionalClaims=null and this SP assignment-required=true after checking the journal/snapshot; preserve drafts/audit, certificates, roles/consent and processor. Existing sessions may persist until expiry; a setting reversal is not immediate session revocation.

## Local host

Stopped the previous development host and started the tested isolated `.runtime/member-workspace/publish/Gateway.Web.dll` on https://localhost:7188 in Development with unchanged private web configuration/local SQL connection. The listener and content root were observed. No database migration or source promotion occurred. The new local content-root-derived data-protection key directory is ignored and differs from the previous host; a fresh login is required rather than relying on old cookies. No key was exported/copied.

SQL baseline before new-member acceptance: one existing Owner draft, Revision 4 / ACL revision 3, four document audit events, one ACL member, no source versions. Its ID remains c1f8fbe7-e1c1-420a-86a5-1f867219266d.

Fresh A login was initiated via the ordinary OIDC code/PKCE flow, with the human entering credentials directly at Microsoft. Acceptance results are pending in this initial record. B was read-only verified as an enabled Member with no direct web app-role assignment (42c1e930-b22f-4a82-b7e0-daa5dd9eebdb); no B permission/mapping was added. Login of an already assigned A alone cannot prove non-role enrollment for B.

## Completed live Member acceptance

The human subsequently completed fresh A and B sign-ins through Microsoft. Browser/API requests in these checks were real, without intercepted responses, test auth or extracted cookies/tokens. Only synthetic metadata was written; no file uploaded.

| Observed check | Result |
|---|---|
| A /auth/session | Target tid, reviewed A oid, roles=[LibraryReader], canManageOwnDocuments=true |
| A private workspace/create | Initially empty despite Owner's existing draft; created eb656259-e8cc-4dd4-b09f-1e87d493c523, Revision 1 / ACL revision 1, zero ACL members/source versions, authenticated A creation audit |
| A -> Owner's known draft and audit GET | Both actual problem responses DOCUMENT_NOT_FOUND, status field 404; Owner data not returned |
| A logout | App logout followed by selecting A in Microsoft's picker; Microsoft displayed signed-out confirmation; proceeded with fresh B login |
| B /auth/session | Target tid, reviewed B oid, roles=[], canManageOwnDocuments=true |
| B private workspace/create | Initially empty despite Owner/A drafts; created 0ebc0f54-59dc-426f-a1f4-6ce764f2ccb2 without role assignment or reviewed owner-directory enrollment |
| B -> A's known draft and audit GET | Both actual problem responses DOCUMENT_NOT_FOUND, status field 404; A data not returned |
| B fresh page load/update | Only own draft listed; reopened Revision 1, changed purpose through real form, saved Revision 2; ACL revision 1 and zero members/source versions |
| Independent SQL readback | All three documents have correct distinct owner oids; A creation and B creation/update audit actors match their authenticated identities; original Owner remains Revision 4 / ACL revision 3 / four audit events / one ACL member |
| Post-login target Graph readback | Complete app-assignment inventory, B direct assignment count still 0; no role/default enrollment was silently added |

Screenshots: [A private draft](2026-10-20-member-a-live-draft.jpg), [B role-free private draft](2026-10-20-member-b-live-draft.jpg). B remains signed in on its own local workspace for review. The earlier 183 automated tests were not rerun/increased for this evidence-only follow-up.

This establishes the bounded live self-service metadata milestone, including one Member with no app role and no pre-enrolled owner-directory entry. It does not establish every tenant user's license/account/conditional-access conditions. Guest rejection, symmetric cross-owner updates/audit and identity-cache edge cases have separately recorded synthetic/SQL/browser test evidence. Live crossed reads exercised A->Owner and B->A; live A->B and Owner->A/B update denials were not manually exercised. No claim that portal metadata tests prove Office crossed-copy denial. The audit export Mode remains Simulated (0); these actual metadata writes are not LiveMip issuance.

G1 partial, G2 deferred/not passed and G6 not passed remain unchanged. No upload/publish/withdraw/Ready/download path was enabled. Recipient selection remains the bounded reviewed Owner/A set; all-tenant people lookup is still unimplemented.
