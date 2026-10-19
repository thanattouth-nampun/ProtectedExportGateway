# Durable SQL checkpoint — T06

This checkpoint adds EF Core SQL Server persistence and an export queue. It is not M1 workflow acceptance or a live MIP release. The Web application does not register this store or expose job creation until server source/ACL/role authorization and lifecycle APIs are implemented.

## Implementation

- `Gateway.Infrastructure` owns SQL rows, EF migrations and `SqlExportQueue`. EF Core/SQL Server/Design and local dotnet-ef are pinned to 10.0.13, compatible with the existing .NET 10 target. SQL provider uses compatibility level 160; no SQL 2025-only JSON feature is required. Microsoft's [provider documentation](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/) covers SQL Server/Azure SQL; this checkpoint tests only LocalDB.
- Documents, versions and ACLs have same-tenant composite foreign keys; the published pointer cannot reference another document's version. Rowversion protects metadata from lost updates. Publication/validation authority is still the T22/T25 service's responsibility; writing a persistence row is not publishing authorization.
- Export jobs store an immutable submission snapshot/digest including requester, mode, source kind, source/dataset immutable version/hash, policy and rights versions. Copy snapshots additionally contain business owner, label, processor protection owner and recipient mapping. No snapshot is exposed as a client request DTO. The report snapshot is a queue input contract; full live report rule/owner semantics remain T07/T09 work.
- Unique `(tenantId, requesterOid, idempotencyKey)` prevents duplicate jobs. Serializable lookup with `UPDLOCK,HOLDLOCK` makes concurrent equal submissions replay the original job. Different digests conflict. HTTP callers must be authorized again before replay and resolve existing snapshots before remapping current settings; this queue does not perform current ACL/publication checks.
- Claim uses a short SQL transaction with `UPDLOCK,READPAST,READCOMMITTEDLOCK` and `UPDATE ... OUTPUT`; database UTC is authoritative. Each claim has a new token and monotonically increasing fence/attempt number. Mutations require exact worker identity, token, fence, expected state and an unexpired lease. Workers must not log tokens; audits use the original request correlation ID instead.
- Expired attempts can be reclaimed; previous attempts close as LeaseExpired. Retries have a 30-second delay and three-attempt cap. Exhausted attempts are failed transactionally. A native process supervisor is still required before live SDK execution: fencing prevents stale SQL commits, but does not stop a native process or clean its files.
- Enqueue, claim, stage transitions and exhaustion commit audit events in the same transaction. Real SQL trigger failures test rollback, including attempt updates. Audit is an application journal, not native Purview audit or a tamper-proof ledger.
- Only stage advancement, simulated completion, retry and failure are writable. There is no artifact/publication API. SQL check constraints reject Ready and Expired in this checkpoint and reject simulated completion in live mode. T10 must replace the conservative state constraint only with the verified binding transaction implemented and tested.

## Local commands and storage

```powershell
./scripts/verify-sql.ps1
./scripts/verify.ps1
```

The SQL suite requires Windows SQL LocalDB on PATH; absence fails explicitly. It accepts no tenant/cloud connection string. Each test creates a uniquely named `GatewayQueueTest_<guid>` database with files below `.runtime/sql-tests`, applies migrations, tests real SQL Server behavior, and drops that test database in teardown. An interrupted process may leave a test database: inspect its exact name/files and confirm it belongs to this suite before deleting it. Do not recursively delete an attached database directory.

The main verification script now includes this suite. CI's Windows runner needs LocalDB; a hosted run has not been performed. Native SDK and Azure gates are separate.

Migration tooling is pinned in `dotnet-tools.json`. Generate reviewable SQL without applying it:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/Gateway.Infrastructure --output infra/sql/gateway-schema.sql
```

`infra/sql/gateway-schema.sql` is a generated preview, not an auto-deploy script. The design-time factory has only a local placeholder database, no credential. Never apply migrations to Azure, a shared database or an existing user database without reviewing exact target, backup/data rollback, cost and authorization. Down migrations remove tables/data; generated rollback SQL is not a data-restoration strategy. No application startup migration runs automatically.

Do not enable SQL retry strategies around these explicit transactions without implementing/reviewing an execution-strategy wrapper and ambiguous-commit/idempotency behavior. Azure managed identity, database grants, connection resiliency and deployment remain T18/T19 work; local integrated authentication is not cloud identity evidence.

## Remaining work

T07/T22 authenticated source/ACL/owner APIs and metadata/audit transactions; durable intake/idempotency and evidence tables in T23/T25; T08 bounded queue admission/fair scheduling and simulated worker; T09/T10 real SDK inspection and immutable artifact publication; T11 supervisor/cleanup/outage recovery; full role screens and live gates. Live scanner, Office cross-copy denial and tenant/Azure tests remain not run. Queue tests cannot establish protection or clean-file evidence.
