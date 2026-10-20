# Approved local web setup — 20 October 2026

This record follows the earlier owner-workspace implementation checkpoint. The human approved its exact private target/change/cost/rollback packet before execution. It does not retroactively change the earlier test evidence.

## Observed setup

- Created the separate single-tenant `ProtectedExportGateway-Web-Local` app and service principal. Client ID: `a834c776-387c-43aa-8d55-3ab62a08acf6`. No MIP processor changes.
- Graph readback confirmed exactly the two approved user role definitions and three assignments: reviewed Owner has LibraryReader and DocumentPublisher; reviewed A has LibraryReader. Assignment-required is enabled; each assignment resource/type matches the new service principal/User.
- Exactly the approved localhost callback/signed-out redirects; implicit grants disabled, no SPA/public-client redirects, no requested Graph/MIP API permissions, no password credentials. A dedicated 14-day non-exportable certificate was registered using its public certificate only. Local key export policy readback was None.
- Added only the approved existing localhost public certificate to CurrentUser/Root. The human confirmed the Windows trust dialog. Existing My certificate/private key and machine root store were preserved.
- Private `.runtime/owner-web/setup-journal.json` records new object, assignment, certificate and trust identifiers for scoped rollback; status VERIFIED_SETUP. Credentials/tokens/private keys are excluded from tracked evidence.
- No Azure resource, deployment, new user/license, admin-consent operation or client secret was created. Estimated additional Azure resource cost remains THB 0; existing subscription/license costs are excluded.

The first script invocation failed syntax validation before mutation and before journal creation. After correction, one creation run completed; it was not rerun. Subsequent read-only checks verified exact role values, assignment resource/types, implicit flags and local key export policy.

## Local database and host

`sqlcmd` inspection of master found no pre-existing GatewayOwnerLocal database. `dotnet ef database update --project src/Gateway.Infrastructure --connection <private local connection>` succeeded, applying all four repository migrations through OwnerDraftAudit. SQL readback confirmed the migrations and both data/log files under `.runtime/owner-web/sql/` in this workspace.

`dotnet run --project src/Gateway.Web --no-restore --urls https://localhost:7188` started in Development using the private local config. The browser rendered the real same-origin application over HTTPS, and its Sign in link reached the configured tenant's Microsoft sign-in page with code flow, PKCE S256 and openid/profile scopes.

## Acceptance boundary

After the human completed Owner sign-in, the browser returned to `/library` and rendered the authenticated owner's name. `/my-documents` loaded through the real publisher-authorized API. No browser API interception or test principal was used in this check. Successful publisher-only writes provide evidence that the server accepted the configured Owner session/role; raw ID tokens, cookies and claims were not extracted.

The browser created one synthetic metadata-only draft, updated its purpose, removed A and restored A through the actual form. SQL readback independently confirmed document `c1f8fbe7-e1c1-420a-86a5-1f867219266d`, the target tenant and reviewed Owner object ID, final Revision 4 / ACL revision 3, exactly one ACL row for reviewed A, zero source versions and no published pointer. Removing A produced Revision 3 / ACL revision 2 with zero ACL rows. Audit contains DocumentDraftCreated (1), DocumentDraftMetadataUpdated (2) and DocumentDraftAclUpdated (3,4), all attributed to the authenticated Owner. Reloading the page and reopening the draft confirmed persistence at Revision 2 before the subsequent ACL checks.

These are real Entra/local SQL metadata operations on synthetic data. The application's export execution mode remains Simulated; audit Mode=0 records that mode and is not a LiveMip claim. ACL edits do not grant RMS rights or publish content. The UI keeps upload/publish disabled and shows the release restriction. Screenshot: [actual Owner draft](2026-10-20-owner-live-draft.jpg).

Live A sign-in/authorization and sign-out/session-boundary behavior have not been manually checked in this run. Earlier 172 automated tests remain separately documented synthetic/disposable-test results; they are not a substitute for those live checks. No new automated suite was run for this evidence-only follow-up.

Subsequent Reader checkpoint: the human attempted A sign-in; the browser shows Need admin approval before returning to the application. Read-only Graph inspection found consent only for Owner, no applicable A/all-principals grant, with A's Reader assignment intact. See [Reader consent runbook](../reader-login-consent-runbook.md) for the separately prepared, unapproved A-only openid/profile consent change. A authentication/API denial/session-boundary tests are not passed by this result. No consent mutation has been performed.

Final Reader update: the human instead selected/approved tenant-wide consent for this app's openid/profile only. One AllPrincipals grant was created and independently verified, preserving existing role assignments, assignment-required and Owner consent. A then actually logged in with LibraryReader only; direct Owner UI/list/known-draft API access was denied. Actual A Sign out ended the local session; later session endpoint returned SESSION_REQUIRED and the Owner route displayed the sign-in screen without draft data. Automatic Microsoft post-logout return was not observed. The runbook now records the approved execution and scope/rollback; the earlier A-only proposal was never executed. No protected artifact/release gate changed.

G1 remains partial; G2 is deferred, not passed; protected intake/scanner G6 is not passed. No upload, publish, Ready or downloadable artifact is enabled. Creating a portal draft will not prove original RMS ownership.

## Scoped rollback

Stop the local host. Use the private journal to remove only the new web assignments, service principal and application, then only its new CurrentUser/My OAuth certificate/key. Remove only the journaled newly added localhost CurrentUser/Root entry; preserve the pre-existing localhost My key/certificate, machine roots and MIP identity. Remove/disable the private web client configuration. Preserve local SQL/audit evidence; migration reversal or database deletion requires separate data-loss review. Existing sessions/issued tokens may persist until expiry; local shutdown prevents further service use.
