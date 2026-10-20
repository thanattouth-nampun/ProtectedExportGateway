# Reader login consent checkpoint — 20 October 2026

## Current status: tenant-wide alternative approved and verified

The human selected and explicitly approved consent once for the tenant, limited to this web app's `openid profile` scopes, after the A-only alternative below was explained. `scripts/setup-tenant-oidc-consent.ps1` is the separate implementation of that decision; the old A-only script/packet was not executed. Its private plan is marked superseded.

The exact approved create payload used the same client/resource IDs below, **consentType AllPrincipals, principalId null, scope openid profile**. Target remains tenant baht.net / 9fbc4f5d-56ec-4074-82f0-69d9a86a7c06 and only ProtectedExportGateway-Web-Local. This consent permits the app to obtain sign-in/profile information for users allowed to sign in; Assignment required continues to restrict app entry. No User.Read, Files/Mail, offline_access, MIP permissions, app roles or tenant consent-policy changes were added. Estimated additional Azure resource cost: THB 0; no resources were provisioned/deployed.

Default read-only preparation and PowerShell parser validation passed. The separately approved mutation run completed VERIFIED, with independent exact grant readback. Post-checks confirmed Assignment required remains enabled, all four existing role/default assignments unchanged and the original Owner Principal consent preserved. Private `.runtime/owner-web/tenant-oidc-consent/execution-journal.json` records the newly created grant ID and approval/scope/readback flags. No A-only consent grant was created.

Rollback for this actual run is deletion of **only the newly recorded AllPrincipals grant ID**, after checking client/resource/scope/type and null principal. Preserve Owner consent, app roles, tenant policy, certificates and data. Existing tokens/sessions can persist until expiry; stopping the local host blocks further service access. The older Principal-scoped rollback description below applies only to the unexecuted historical proposal.

### Actual Reader/session checks

- Human completed a fresh ordinary A login after consent; the real app showed peg-test-a without Owner navigation. No API interception or test principal was used.
- Browser navigation to `/auth/session` returned the target tenant, A's reviewed object ID and exactly `["LibraryReader"]`, mode Simulated. No raw cookie/token was extracted.
- Direct `/my-documents` navigation displayed **Document publishing access is required**, with no Owner draft/ACL/history displayed. [Actual denial screenshot](evidence/2026-10-20-reader-owner-workspace-denied.jpg).
- Direct browser requests to `/api/my/documents` and `/api/documents/c1f8fbe7-e1c1-420a-86a5-1f867219266d` both rendered `{"code":"ACCESS_DENIED"}`. This is real backend denial, separately from the UI guard. The response body's code was observed; no network-status instrumentation was used. Code maps this cookie access-denied response to 403.
- Library still reports unavailable; no source/copy is downloadable. A does not see the Owner's draft left in the prior session.
- Actual Sign out posted through the app and reached Microsoft's A account picker. Selecting A displayed **You signed out of your account**. Subsequent `/auth/session` returned `{"code":"SESSION_REQUIRED"}` and `/my-documents` rendered the sign-in screen without retained draft data. Microsoft did not visibly redirect automatically to the local signed-out callback in this run; the local session check was a deliberate navigation afterward. Cookie deletion/local session denial passed; automatic post-logout callback return remains unproven.
- Automated cross-identity cache tests remain separate evidence; this manual check does not prove every multi-tab/expiry/session-revocation case. G1 partial, G2 deferred and G6 not passed remain unchanged.
- Final SQL readback confirmed the Owner synthetic draft remains Revision 4 / ACL revision 3, four audit events, one ACL member and zero source versions after A's denial/logout checks. [Signed-out screenshot](evidence/2026-10-20-reader-signed-out.jpg). The denial screenshot was captured immediately after navigation and is functional-denial evidence, not settled-layout/device-performance evidence.

No product behavior/dependency versions changed for this follow-up. Checks were parser validation, read-only preparation, actual Graph mutation/readbacks, browser acceptance and evidence JSON/diff checks; the earlier 172 automated test count was not rerun or increased.

## Observed blocker

The in-app browser is on the configured tenant's Microsoft sign-in page for `peg-test-a@baht.net`, showing **Need admin approval** for ProtectedExportGateway-Web-Local. This is before BFF session establishment; it is not an Owner API authorization result.

Read-only Graph inspection found the web application's requestedResourceAccess empty, the localhost redirects unchanged and implicit grants disabled. Its existing OAuth grant is Principal-scoped to the reviewed Owner with only `openid profile`. There is no applicable A grant or AllPrincipals grant. A still has exactly the LibraryReader app-role assignment and no DocumentPublisher assignment. The earlier inspection also found a default zero-role assignment for Owner in addition to its two named roles; no removal or expansion was performed.

The observed consent gap is consistent with the sign-in failure. The exact tenant user-consent policy responsible has not been inspected or changed. Assignment controls access to the application; consent authorizes the requested sign-in/profile scopes. They are distinct controls.

Screenshot: [actual consent blocker](evidence/2026-10-20-reader-consent-blocked.jpg).

## Historical A-only proposal — superseded, not executed

| Field | Exact target |
|---|---|
| Tenant | 9fbc4f5d-56ec-4074-82f0-69d9a86a7c06 / baht.net |
| Web client app ID | a834c776-387c-43aa-8d55-3ab62a08acf6 |
| Client service principal object ID | 3cd6f447-4747-403a-8741-a034f3870e9c |
| Principal | peg-test-a@baht.net / 320cf98c-e4b6-4831-9b40-2aab8dea3d5a |
| Resource | Microsoft Graph OIDC scopes; service principal f8faa1d5-b6b1-4588-82ea-d90abbf28e4c |
| Operation | One new oauth2PermissionGrant, consentType Principal |
| Scope | openid profile only |
| Additional Azure resource estimate | THB 0; existing license/subscription costs excluded |

This permits sign-in/basic profile for A to this app. It is not tenant-wide consent, Graph User.Read/Files/Mail permission, offline_access, MIP permission, new app-role assignment or a tenant-policy change. Existing Owner consent, processor, licenses and resources remain untouched. The original web setup explicitly excluded admin consent, so its prior approval does not authorize this new grant.

`scripts/setup-reader-oidc-consent.ps1` defaults to read-only Entra preparation and writes an ignored private packet in `.runtime/owner-web/reader-consent/`. Its preparation mode was executed successfully, after PowerShell parser validation; no Entra mutation occurred. The packet contains exact `create.request.json` and `change-plan.json`. It rejects an existing applicable consent/incomplete inventory instead of expanding or reusing it silently.

Historical command, **do not execute for the current tenant-wide decision**:

```powershell
./scripts/setup-reader-oidc-consent.ps1 -ApprovedConsent
```

The mutation path has not been run or live-tested. It rechecks the target/user/Reader/resource/scope boundary, records a private execution journal before POST, and independently reads back the created ID/scope tuple. It refuses repeat creation when the journal exists; ambiguous responses require read-only inspection, not automatic retry. A permission failure must not trigger broader grants, tenant-policy changes or a new admin-consent workflow.

## Rollback

Record the newly created grant ID in the ignored execution journal. Revoke only that grant using `DELETE /v1.0/oauth2PermissionGrants/{recorded-new-id}` after checking its client/principal/resource/scope against this packet. Preserve the pre-existing Owner grant and app roles. Stop the local host if immediate service access must stop; issued tokens/sessions can persist until expiry, and deleting consent is not immediate session revocation. No database or source-file deletion is part of rollback.

## Next checks after consent

1. Human retries Login as A in the official Microsoft UI; never provide credentials through chat.
2. Verify actual A session, absence of Owner navigation and no retained Owner draft/selection state.
3. Navigate directly to `/my-documents` and the publisher API; require denial and no owner metadata. No browser token/cookie extraction.
4. Verify published catalog remains unavailable and no artifact is released. Capture the actual result separately from intercepted-browser and test-principal tests.
5. Continue T23 protected quarantine/evidence binding preparation, then authorized content-inspection/scanner feasibility T24/T25. G1 remains partial; G2 deferred; G6 not passed. No upload/publish/download enablement follows merely from Reader Login.

## References

- [Create delegated consent grant](https://learn.microsoft.com/en-us/graph/api/oauth2permissiongrant-post?view=graph-rest-1.0): Principal scopes the grant to one user; clientId/resourceId are service principal object IDs.
- [OpenID/OAuth app consent behavior](https://learn.microsoft.com/en-us/entra/identity/saas-apps/openidoauth-tutorial): an administrator's own consent can leave regular users unable to consent/sign in.
