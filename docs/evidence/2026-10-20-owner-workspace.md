# Owner workspace implementation evidence — 20 October 2026

## Implementation versus evidence

Implemented: SQL-backed private metadata Draft/ACL APIs, document-bound transactional audit migration, React owner forms/history and server session/OIDC certificate configuration. Scope and commands: ../owner-workspace-checkpoint.md.

Real local SQL evidence: isolated SQL Server LocalDB databases ran actual migrations, persistence/restart, owner/tenant/role isolation, ACL validation, concurrent If-Match edits, source-presence mutation rejection and injected audit-failure rollback. No SQLite/InMemory substitution. Existing queue/lease tests remained included. Test identity records/data are synthetic; this is not real user authentication evidence.

HTTP evidence: test-assembly-only principals validate CSRF, roles, tenant binding, unknown JSON rejection, server-derived owner, exact ETag forwarding, structured errors and unavailable dependencies. Configuration composition resolves the real SQL service but deliberately does not open a DB. A real OIDC handler challenge is tested with static metadata for code/PKCE/correlation-cookie behavior; no user code redemption. Certificate assertions are cryptographically validated with ephemeral in-memory test certificates, including audience/endpoint rejection and short expiry. These are not live Entra success claims.

UI evidence: Chromium uses intercepted synthetic API responses. Checks cover draft creation/reload, explicit ACL selection, If-Match stale conflict/reload, identity-bound form clearing, dependency errors, unconfirmed-save blocking, keyboard checkbox behavior and 390px reflow. Desktop/mobile captures show synthetic identities only. No accessibility certification, measured real-device performance or live SQL-to-browser claim.

## Commands and final results

Results are recorded in the accompanying JSON after final checks. Executed groups:

- `dotnet restore ProtectedExportGateway.slnx --locked-mode`.
- `scripts/verify-sql.ps1` — 22 passed on isolated LocalDB (15 existing queue + 7 owner cases), including migration snapshot/rollback/reapply.
- `dotnet test tests/Gateway.IntegrationTests --no-restore` — owner/session/CSRF/OIDC configuration and assertion checks.
- Final combined result: **28 HTTP tests passed**, including the streamed metadata-size rejection. A focused follow-up also checked case/trailing-slash aliases of the same route.
- `dotnet test tests/Gateway.UnitTests --no-restore` — 101 passed.
- `npm run contracts` against actual exported BFF OpenAPI; `npm run build`; `npm test` — 7 projection tests passed.
- `npm run test:browser` — owner plus existing workspace browser checks.
- Final browser result: **14 passed**; with 101 unit, 22 SQL and 7 frontend unit tests, **172 unique tests passed** across these suites.
- Local `dotnet publish -c Release --no-restore -p:SkipClientBuild=true -o .runtime/owner-web/publish` after the current React build; verify bundled index.html and no spike.html/private config. SkipClientBuild avoids a redundant npm restore; assets were built immediately beforehand. This is a local release folder, not Azure deployment.

Initial failures were corrected before final verification: strict JSON binding returned 500 in Development instead of structured 400; generated integer unions required explicit Number conversion in the UI; the OIDC fixture needed early host settings and an explicit static metadata manager. A mobile full-page screenshot was recaptured after scrolling to top to avoid an off-viewport fixed-link capture artifact. No failed result was counted as acceptance.

A metadata-size test exposed that PostAsJsonAsync can omit Content-Length and TestServer lacks Kestrel's body-size feature. Added an actual bounded read stream in addition to the Kestrel limit; the streamed oversized-body case now rejects before persistence. It is included in the final HTTP suite, not counted as passing at its initial failure.

## Gates and next action

The earlier simple MIP experiment and user-confirmed Restricted Editor UI evidence remain separate. G1 is partial, G2 behavior tests remain deferred by the user, G6 not passed. This checkpoint enables no upload, source promotion, publish/withdraw, Ready state or download. Reports and other modules are retained; their pending routes are unchanged.

No tenant mutation, web registration, user-role grant, certificate enrollment/trust, Azure provisioning or deployment occurred here. A local web registration proposal with exact identity/role/certificate/redirect scope, THB 0 new resource cost and rollback is ready in ignored files. Next requires separate human approval for that new web identity, then real login/role/session-to-SQL checks. Do not reuse the MIP processor as the web identity.
