# Deployment review boundary — prepared, not executed

No Azure/Entra configuration has been changed. This checkpoint has no validated Bicep deployment and must not be deployed as a working live gateway. Use docs/11 as the target architecture. Complete the independent SQL/intake/worker implementation and live MIP/scanner gates first.

Before requesting authorization, produce a review package containing:

1. Tenant/subscription IDs (controlled metadata), resource group, region, ingress audience, management route and named owner.
2. Exact resource inventory, SKUs/instance counts/runtime/images/operating hours; include VM disks, SQL backup/storage, private endpoints/DNS, logs/retention, Blob versions/soft-delete and optional management service costs. Price the selected region/offer using the Azure Pricing Calculator; record date, currency, assumptions, monthly ceiling and budget recipients. No monetary estimate is invented before these inputs exist.
3. Reviewed Bicep/local validation and authorized what-if, scoped managed identities/RBAC/SQL grants, quarantine upload-only web scope, source create-only application writes and exact-version reads; no credentials in parameters/outputs.
4. A separate tenant change list: web/processor registrations, certificate credentials, redirect/logout URLs, app-role assignments, required consent/label publication. Existing tenant/subscription does not authorize those changes.
5. Versioned web/worker/helper artifacts, DB migration and Data Protection/key/certificate strategy. No browser MIP/OAuth storage. Shared key persistence must pass App Service restart tests.
6. G0/G1/G2/G6 evidence, commercial-cloud endpoint review, private DNS/data-plane RBAC negatives, scanner/helper process privilege and supervisor tests. Never grant SuperUser or elevate the web/MIP worker to get a demo through.
7. Rollback: previous compatible web/worker builds and schema/config compatibility, stop admission, drain/fence attempts, preserve audit/evidence, expire unsafe bindings and keep candidates private. Never roll DB state back to make an older artifact downloadable.
8. Teardown: explicitly approved resource IDs only; preserve approved evidence, remove credentials/role assignments deliberately, account for retained Blob versions/SQL backups/Key Vault soft-delete and residual costs. Resource-group deletion requires separate destructive-action authorization.

Only after this concrete package is ready should the environment owner approve the exact target/cost/change set. A design discussion, package restore, IaC build or what-if is not deployment acceptance. G5/G7 cloud/restart/native/RBAC/intake/client behavior must be rerun after any approved deployment.
