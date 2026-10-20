# Environment worksheet — 19 October 2026

20 October update: the explicitly requested one-recipient Restricted Editor probe acquired real app-only policy/RMS tokens and matched owner-preserving source/output rights inspection. See evidence/2026-10-20-restricted-editor-probe.md. Owner/A enabled Member records were verified; A has no assigned licenses. Office entitlement, UI/effective access and G2/G6 remain unproven. Earlier NOT_RUN statements below describe the 19 October checkpoint and are superseded only for this bounded experiment.

ADR-013 update: standalone content intake; SharePoint/OneDrive connectors deferred. Protected source ownership, original-owner processor delegation and encrypted-content scan/validation are new live proving obligations; all remain unverified. See owner-only-spike-runbook.md for target fields and required approval. Local checks use only synthetic fixtures; no certificate lookup or tenant authentication was invoked for the owner-only path.

Source of truth: current repository and the user's current instructions. No earlier chat history or memory was used.

| Item | Observed status | Responsible / next proof |
|---|---|---|
| Tenant/subscription target | Read-only authenticated Graph/ARM inspection verified user-selected tenant/domain and Enabled subscription binding; exact metadata in ignored local files | No provisioning/cost authorization; see owner-only setup review packet |
| Target/cost/deploy authorization | Dedicated processor setup approved and executed; Azure provisioning/deployment and live processing remain unapproved | See evidence/2026-10-19-processor-setup.md; future resources require separate target/cost/rollback review |
| Application sources | Created in this milestone; baseline contained documents/prototype only | Developer |
| Git | Local `main` initialized and initial commit pushed to user-specified GitHub repository in the earlier authorized step | Current implementation changes are local; no automatic push in this checkpoint |
| Host | Windows x64; SDK 10.0.300, runtime/ASP.NET 10.0.8 | Native-load check passed; full SDK context/auth pending |
| .NET inventory | `dotnet --info` partly failed reading Service Control Manager | No elevation for scanner/web/worker was attempted |
| Node/npm | 24.16.0 / 11.13.0 | Compatible engines/peer restore/build verified |
| NuGet/npm network | Sandbox DNS/socket/process restrictions; approved local restore/test commands worked outside sandbox | No tenant permission changes |
| SQL Server/LocalDB/sqlcmd | Now available on PATH; connected to SQL Server 2025 Express LocalDB 17.0.4025.3 | T06 isolated SQL migrations/queue/transaction tests run; see sql-queue-checkpoint.md and current evidence; no SQLite/InMemory substitution |
| Azure CLI/Bicep | Target-specific CLI sign-in and Graph/ARM metadata reads completed; standalone `bicep` not found, CLI-managed Bicep not inspected | Approved processor directory setup verified; no what-if, resource provisioning or deployment |
| Web registration, roles, callbacks, credential | Not configured | Tenant admin; local secret store, no secrets through chat |
| Processor app/certificate/consent | Approved dedicated app/SP, 14-day non-exportable certificate and three exact roles created and read back | Private journal has IDs/rollback inventory; MIP authentication remains NOT_RUN |
| RMS readiness/license/labels | Unknown | Information protection admin confirms feature entitlement, scope and publication |
| O1/O2 and A/B IDs/recipient mappings | Unknown | Operator-provisioned same-tenant mappings and evidence |
| Expected protection owner | Unknown | Prove actual service principal ownership in G1 |
| Office clients/caches | Not inspected | G2 isolated allow/deny/edit/save/cross-open controls |
| AV/helper availability | Not inspected, not invoked | G6 scanner engine/signature/verdict/hash/privilege proof; exit0 alone rejected |
| Browser evidence | Playwright1.64.1 Chromium; current list/detail tests run without GPU flags | Historical renderer evidence is retired; see current evidence |
| Browser/mobile load and reflow profiles | Full representative profiles not agreed/measured | G7 still requires named devices/browsers/network; GPU/FPS no longer applies |

G0 remains incomplete for live file work. The approved processor app/certificate/three-role setup is verified; no Azure resource, label, network policy, AV exclusion, scanner elevation or billing activation changed. See separate processor setup evidence for directory changes and the remaining authorization boundary.
