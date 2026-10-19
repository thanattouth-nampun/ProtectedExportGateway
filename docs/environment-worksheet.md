# Environment worksheet — 19 October 2026

Source of truth: current repository and the user's current instructions. No earlier chat history or memory was used.

| Item | Observed status | Responsible / next proof |
|---|---|---|
| Tenant/subscription existence | User-confirmed; no target IDs inspected | Environment owner supplies non-secret target metadata locally |
| Target/cost/deploy authorization | Not granted; no cloud/tenant mutation attempted | Prepare concrete resource/region/SKU/cost/rollback review before approval |
| Application sources | Created in this milestone; baseline contained documents/prototype only | Developer |
| Git | `git.exe` now on PATH; repository still has no `.git` | No repository initialization or remote changes performed |
| Host | Windows x64; SDK 10.0.300, runtime/ASP.NET 10.0.8 | Native-load check passed; full SDK context/auth pending |
| .NET inventory | `dotnet --info` partly failed reading Service Control Manager | No elevation for scanner/web/worker was attempted |
| Node/npm | 24.16.0 / 11.13.0 | Compatible engines/peer restore/build verified |
| NuGet/npm network | Sandbox DNS/socket/process restrictions; approved local restore/test commands worked outside sandbox | No tenant permission changes |
| SQL Server/LocalDB/sqlcmd | Now available on PATH; connected to SQL Server 2025 Express LocalDB 17.0.4025.3 | T06 isolated SQL migrations/queue/transaction tests run; see sql-queue-checkpoint.md and current evidence; no SQLite/InMemory substitution |
| Azure CLI/Bicep | `az.cmd` now on PATH; standalone `bicep` not found, CLI-managed Bicep not inspected | No Azure sign-in, what-if, deployment or resource inspection |
| Web registration, roles, callbacks, credential | Not configured | Tenant admin; local secret store, no secrets through chat |
| Processor app/certificate/consent | Not configured or inspected | Tenant admin; separate app from web; no automatic consent |
| RMS readiness/license/labels | Unknown | Information protection admin confirms feature entitlement, scope and publication |
| O1/O2 and A/B IDs/recipient mappings | Unknown | Operator-provisioned same-tenant mappings and evidence |
| Expected protection owner | Unknown | Prove actual service principal ownership in G1 |
| Office clients/caches | Not inspected | G2 isolated allow/deny/edit/save/cross-open controls |
| AV/helper availability | Not inspected, not invoked | G6 scanner engine/signature/verdict/hash/privilege proof; exit0 alone rejected |
| Browser evidence | Playwright1.64.1 Chromium; current list/detail tests run without GPU flags | Historical renderer evidence is retired; see current evidence |
| Browser/mobile load and reflow profiles | Full representative profiles not agreed/measured | G7 still requires named devices/browsers/network; GPU/FPS no longer applies |

G0 remains BLOCKED for live work. Independent implementation continued. No resource, app registration, role/consent, label, network policy, AV exclusion, scanner elevation or billing activation was changed.
