# 11 — Azure resources, hosting and deployment plan v4.2

> Current decision v4.2 (19 October 2026): ADR-012 removes the 3D renderer. Use the searchable HTML list/detail workspace with React/TypeScript and .NET BFF. Historical 3D evidence is not a current deliverable. All functional, security, native SDK and Azure controls remain required.

สถานะ: proposed architecture; no subscription access, resource creation, pricing quote or deployment has been performed. Azure hosting เป็น deliverable ของโปรเจ็กต์ แต่ IaC preparation ไม่เท่ากับ permission สร้างค่าใช้จ่ายที่ยังไม่กำหนด

## Proposed resource inventory

| Resource | Purpose | Baseline and cost driver |
|---|---|---|
| Resource Group | แยก lifecycle ของ showcase | tags Project/Owner/Environment/ExpiryDate/CostCenter; not a security boundary |
| App Service Plan + Windows Web App | .NET API/session/BFF + compiled React + HTTPS/OIDC | one web release/origin, supported SKU/runtime/VNet; no separate production Node service |
| Windows VM + managed OS/data disk + NIC | native MIP/ingestion worker, private staging and scoped AV helper | supported OS/.NET/native+scanner prerequisites, 1 active task; compute/disk/Windows and any selected security entitlement costs |
| Azure SQL logical server + database | jobs, document ACL, metadata, evidence and audit | Entra database users for MI, low-volume suitable tier after estimate; serverless only if region/support/wake-up behavior acceptable |
| GPv2 Blob Storage account | quarantine/source/output containers | private containers, version/hash binding, scoped managed identities; intake operations, retained bytes/versions and transfer billed |
| Key Vault | separate MIP and web app certificate credentials | RBAC and private endpoint; retrieval operations/cert lifecycle; private-key retrieval requires correct permission |
| VNet, integration/VM/endpoint subnets, NSGs, private DNS | private service access | do not combine App Service integration and endpoint subnets; size/feature requirements verified in IaC |
| Private endpoints | SQL, Blob, Key Vault | per endpoint/hour/data costs; private DNS/links included in design |
| App Insights + Log Analytics | web/worker health, failures, queue/cleanup visibility | sampling/retention/capacity and sensitive-data filters; ingestion/retention costs |
| Budget + alerts | spending visibility | choose supported offer/scope, notify actual owner; alert is not a hard cap |

Deployment/operator access: prefer an existing approved private management route; if none, include and price a compatible Azure Bastion option and its subnet/access prerequisites before provisioning. Do not open public RDP/SSH as fallback. No public IP on worker by default.

No ACR, AKS, Front Door, API Management, Service Bus, Redis or Functions in baseline. Add only for demonstrated requirement. Container Apps/Linux would be a different validated worker architecture, not an automatic replacement for Windows native requirements.

Upload adds no automatic cloud malware-scanning subscription: candidate is Windows Defender Antivirus/helper on VM, subject to availability/permissions/signature/licensing verification in G6. If replaced by Defender for Storage, first review extra service/scan/event/log costs, regional/private connectivity and authoritative result binding; do not enable or bill the optional service implicitly.

## Hosting decision

App Service serves compiled React and .NET API/session/BFF/intake in one artifact. Node LTS is build tooling. The browser uses an HTML list/detail workspace. VM stays the native MIP/intake/scanner host. G5/G7 validate cloud assets/routes/session after native gates; no vendor certification implied.

App Service worker/WebJobs/Windows containers remain alternatives only after a dedicated compatibility/credential/filesystem/lifecycle spike. Do not deploy native worker into web process as a quick workaround.

Local development is supported before subscription access; use local SQL Server-compatible DB and filesystem adapters. Cloud completion requires real Azure resources and G5, not a local port exposed with a tunnel.

## Identity and access matrix

| Identity | Blob | SQL | Key Vault | MIP |
|---|---|---|---|---|
| Web managed identity | export-container Data Reader; reviewed quarantine upload-only custom data role, no source access | explicitly provisioned DB user + metadata/ACL/intake/job/audit schema grants | read web credential only, if confidential OIDC client needs it | none |
| Worker managed identity | quarantine read/cleanup; source promotion custom read/write-no-delete; export Data Contributor | worker-specific grants for intake/source/evidence/queue/cleanup | processor credential only | MI not assumed to replace processor identity |
| MIP processor app | no Blob permission by default | none | credential retrieved by worker MI | documented policy/content application permissions, proven G1 |
| Deployment principal | provision resources/RBAC within approved scope | migrations/bootstrap only | controlled cert provisioning | tenant consent/labels are separate admin work |
| Ordinary A/B | no direct data-plane access | none | none | named recipient only on own copy |

DB `db_owner` ไม่ให้ runtime identities; prepare specific grants/schema/procedures and migration identity. Azure RBAC Contributor does not substitute Blob data permissions or contained SQL database authorization.

V3 changes source-worker read-only baseline: trusted ingestion must create new source blobs. Review exact custom data-role actions and scope in IaC; create-only conditions enforced in application do not mean write RBAC itself is write-once. Export stage has no reason to call source mutation APIs; pin historical version/hash and test tampering denial. If separate ingestion identity/process on same VM is used, record that VM compromise can expose attached identities; don't claim it is a hard host security boundary.

Key Vault public certificate metadata alone cannot sign MSAL requests. Configure access to the private-key-bearing certificate/associated secret or approved non-exportable signing integration; pin retrieval strategy and rotate/test in G5. Do not put PFX bytes/password into Bicep parameters, logs or repo. Scope web and worker access separately; never give Key Vault Administrator to runtime.

## Network and app entry

- Browser → App Service HTTPS endpoint; single-tenant Entra + app authorization. Select demo audience/network restriction before deployment; public URL is not public content access.
- App Service VNet integration provides outbound connectivity to SQL/Blob/Key Vault private endpoints. It does not make incoming web traffic private automatically [S18].
- VM in worker subnet with no public management endpoint. Native RMS/Entra/policy connections still need controlled outbound DNS/TLS/proxy access according to current vendor endpoints.
- SQL/Blob/Key Vault public network paths disabled after private DNS/reachability verified by both web and worker identities. Do not lock out management bootstrap without tested private admin path.
- Validate name resolution to private endpoint addresses from actual worker/App Service context; TCP 443 alone does not prove OAuth/TLS/policy access.
- Set trusted forwarded headers/cookie/TLS behavior for App Service ingress, security headers, health endpoint access and supported .NET publish architecture.

## Blob integrity and retention

Enable versioning on the selected storage account type and record source/output version IDs. Source version references cannot be updated underneath a queued job. Upload candidate names with create-only conditions, capture ETag/version ID and verify actual content hash before Ready.

Owner uploads stream to unique private quarantine objects ≤10 MiB with measured hash; scanner/OOXML evidence binds those exact bytes before new source version promotion. Reader catalog/copy APIs require owner Published current version plus real Eligible evidence. Upload/scan completion alone never publishes. No web endpoint returns quarantine/source bytes or SAS, even to Owner.

Download reads exact recorded version with conditional checks into ≤10 MB verified buffer before sending bytes; never reads mutable latest blob or uses a public/user SAS. Azure encryption-at-rest protects storage layer but does not replace MIP per-recipient protection.

Worker cleanup marks SQL Expired before deleting. Blob lifecycle is backstop only and does not provide minute-precise TTL. Configure previous versions, soft delete/backups and retention intentionally: deleted outputs can remain recoverable and billed even when application access is denied. Separate source-retention policy so export cleanup cannot delete originals.

Add quarantine cleanup target 24h after terminal/abandoned intake, independent from export 60-minute TTL and retained source-version policy. Source promotion/SQL failure leaves private orphan; no pointer means no publication. Publish/withdraw effects are application access changes, not deletion or recall of downloaded protected copies.

## Infrastructure deliverables

```text
infra/bicep/main.bicep
infra/bicep/modules/{network,web,worker,storage,sql,keyvault,monitoring}.bicep
infra/bicep/parameters/demo.example.bicepparam
docs/deployment-runbook.md       # implementation deliverable, not yet created
docs/cost-estimate.md            # populated after actual region/SKUs selected
docs/evidence/G5-<run-id>.md      # actual run only
```

Templates should set public storage access off, TLS requirements, managed identities, scoped role assignments, diagnostic filters, private networking, tags and deploy-time switches explicitly. Do not retrieve or echo private keys in deployment outputs. Entra app registration/consent/labels are separate tenant steps, not automatically covered by a resource group deployment.

## Provision/deploy sequence

1. Confirm subscription/tenant/region/resource names/provider quotas plus publisher roles/scanner prerequisites; complete G0/G1/G2 and local scanner feasibility where available. If local scanner unavailable, review/authorize minimal Azure VM spike before full environment; never bypass G6 to publish.
2. Prepare/validate Bicep locally; render parameters without secrets and run authorized what-if/preview. Review ownership, ingress audience, security plan, cost worksheet and teardown behavior.
3. Obtain concrete provisioning authorization for the selected target/cost envelope. This document itself has not created that authorization's missing parameters.
4. Provision network, private access/admin route, storage/SQL/Key Vault and identities; bootstrap SQL DB users/schema/migrations with deployment principal.
5. Provision/configure VM and install supported runtime/native prerequisites from approved sources; provision certificates out-of-band and test scoped MI retrieval.
6. Publish web/worker/scoped helper immutable builds; set identity/mappings/scanner config out of repo, validate private access and upload O1/O2 synthetic files through intake→scan→Owner publish.
7. Run G5 and final G6/I01–I16 live tests plus report/A/B crossed-copy/restart/role/ACL/Blob-SQL/cleanup/redaction cases; fake seeded success does not prove real uploads safe.
8. Record resource IDs (controlled evidence), costs/limits and operator runbook; provide owner-reviewed demo URL and known limitations.

Creating a private endpoint can still incur cost before application code is ready. Do not keep an unneeded duplicate environment to work around a test failure.

## Cost worksheet — not a quotation

No currency amount is invented here. For actual chosen region/offer/SKUs use [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/) and record the estimate date/assumptions:

| Item | Record before deploy |
|---|---|
| App Service | SKU, instance count, operating hours charged per month |
| VM | size, Windows image/license, running hours vs deallocated hours, disk costs |
| SQL | provisioned/serverless min/max, compute hours, storage/backups, pause/wake assumptions |
| Blob | source/export GB, retained versions/soft-delete, operations and transfer |
| Intake security | quarantine/staging capacity/operations, AV update/compute overhead and actual security entitlement; optional cloud scanning price only if selected |
| Network | number of endpoints, DNS, processing/egress, management path/Bastion if needed |
| Secrets/monitoring | Key Vault operations, log GB/day, retention, diagnostics volume |
| Other subscriptions | Purview/Microsoft 365 entitlements; do not imply included in Azure compute cost |

Set an owner-approved budget and alerts (suggested thresholds 50/80/100%) with a documented response. Budgets notify; they do not automatically stop resources [S23]. Cost data is delayed, so an alert is not an immediate hard spending limit.

For low spend: run one small supported environment, schedule/approve VM deallocation outside demos, review SQL/App Service still-billed costs, remove unneeded diagnostics/versions within retention policy, and delete approved demo resources at end. Stopping VM guest OS is not equivalent to deallocation; disks and other resources can remain billed.

## G5 — Azure acceptance

- App Service sign-in/callback/logout/TLS work in actual endpoint; tenant/role/document ACL enforced
- MI uses only allowed Blob/SQL/Key Vault scope: web quarantine upload/export read, no source access/export write; worker scoped intake/promotion/cleanup; create-only/pinned-source substitution checks pass
- Native MIP both report and UDP document paths pass on VM with recorded versions
- A/B crossed copies denied on tested clients; exact output/source blob versions/hashes and recipient rights evidenced
- App recycle and VM worker restart/hang/retry do not lose jobs or publish unverified copies
- Missing private DNS/Key Vault/SQL/Blob dependency results in unavailable/failed status and no raw fallback
- Cleanup/retention/backups behavior and restore with mismatched bindings tested fail closed
- Log redaction/retention/cost settings, budget notification configuration and teardown plan recorded
- Owner upload/scanner helper/OOXML bounds/metadata/ACL/publish/version/withdraw pass on actual Azure; role/owner spoof and fake/unknown/exit0-remediated cases fail closed; no whole-app admin privileges

Status now: NOT RUN. IaC validation success alone cannot mark G5 PASS.

## Rollback and teardown

Rollback web/worker build together with compatible schema/config versions; preserve evidence and refuse incompatible Ready bindings. Do not roll back DB blindly to make old blobs downloadable. Keep privileged migration access separate.

At demo end export approved sanitized evidence, stop admission, drain/cancel jobs, expire download bindings, remove test artifacts according to retention and revoke unused credentials/role assignments. Deleting a resource group is destructive: verify it contains only intended demo resources and preserve approved data before explicit teardown. Key Vault soft-delete/purge protection, retained backups and non-resource-group Entra apps have separate lifecycles; document residuals and costs.
