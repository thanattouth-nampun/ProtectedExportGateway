param([Parameter(Mandatory)][ValidatePattern('^[0-9A-F]{40}$')][string]$ExistingHttpsThumbprint)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskRoot = Join-Path $taskRepo '.runtime/owner-web'
New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
if (Test-Path -LiteralPath (Join-Path $taskRoot 'change-plan.json')) { throw 'Existing web proposal found; review it instead of replacing reviewed IDs.' }
$taskHttps = Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$ExistingHttpsThumbprint)
if ($taskHttps.Subject -ne 'CN=localhost' -or $taskHttps.NotAfter -le (Get-Date)) { throw 'A valid reviewed localhost HTTPS certificate is required.' }
$taskTarget = Get-Content -LiteralPath (Join-Path $taskRepo '.runtime/owner-only-target.json') -Raw | ConvertFrom-Json
$taskIdentities = @(Get-Content -LiteralPath (Join-Path $taskRepo '.runtime/owner-only-inventory/probe-identities.json') -Raw | ConvertFrom-Json)
$taskOwner = @($taskIdentities | Where-Object { $_.userPrincipalName -eq 'thanattouth@baht.net' -and $_.accountEnabled -and $_.userType -eq 'Member' })
$taskA = @($taskIdentities | Where-Object { $_.userPrincipalName -eq 'peg-test-a@baht.net' -and $_.accountEnabled -and $_.userType -eq 'Member' })
if ($taskTarget.TenantId -ne '9fbc4f5d-56ec-4074-82f0-69d9a86a7c06' -or $taskOwner.Count -ne 1 -or $taskA.Count -ne 1) { throw 'Reviewed target/identities are required.' }
$taskRoles = @(
    [ordered]@{allowedMemberTypes=@('User');description='Request eligible protected copies after document ACL checks';displayName='Library reader';id=[guid]::NewGuid().ToString();isEnabled=$true;value='LibraryReader'},
    [ordered]@{allowedMemberTypes=@('User');description='Manage private drafts owned by the signed-in identity';displayName='Document publisher';id=[guid]::NewGuid().ToString();isEnabled=$true;value='DocumentPublisher'}
)
$taskRequest = [ordered]@{displayName='ProtectedExportGateway-Web-Local';signInAudience='AzureADMyOrg';requiredResourceAccess=@();
    web=@{redirectUris=@('https://localhost:7188/auth/callback','https://localhost:7188/auth/signed-out');implicitGrantSettings=@{enableIdTokenIssuance=$false;enableAccessTokenIssuance=$false}};appRoles=$taskRoles}
$taskPlan = [ordered]@{status='PROPOSAL_NOT_APPROVED';tenantId=$taskTarget.TenantId;defaultDomain=$taskTarget.VerifiedDomain;
    appCreateRequest=$taskRequest;servicePrincipal=@{createNew=$true;appRoleAssignmentRequired=$true};
    certificate=@{store='CurrentUser/My';rsaBits=2048;hash='SHA256';keyExportPolicy='NonExportable';days=14;publicKeyRegistrationOnly=$true;separateFromMip=$true};
    localHttpsTrust=@{existingCertificateThumbprint=$ExistingHttpsThumbprint;subject='CN=localhost';targetStore='CurrentUser/Root';proposeTrustExistingPublicCertificateOnly=$true;requiresExplicitApproval=$true;machineRootChanges=$false;privateKeyExported=$false;rollback='Remove only newly added exact thumbprint from CurrentUser/Root; preserve existing My certificate/key'};
    assignments=@(@{principalId=$taskOwner[0].id;principal='owner';roleId=$taskRoles[0].id;role='LibraryReader'},@{principalId=$taskOwner[0].id;principal='owner';roleId=$taskRoles[1].id;role='DocumentPublisher'},@{principalId=$taskA[0].id;principal='recipient-a';roleId=$taskRoles[0].id;role='LibraryReader'});
    oidcScopes=@('openid','profile');graphPermissions=@();mipPermissions=@();clientSecretsCreated=0;
    estimatedNewAzureResourceCostThb=0;newAzureResources=0;costAssumptions='Existing local Windows host/LocalDB; no new licenses, users, cloud resource or deployment; Office entitlement remains unverified';
    rollback=@('Record all new app/SP/key/assignment IDs before further actions','Stop the local web host; delete only recorded role assignments, new web SP/app and exact CurrentUser/My certificate','Remove only this web config and DPAPI key namespace if retired; do not remove MIP processor objects, source files or other apps','Issued browser sessions must be ended; tenant audit records may persist');
    excluded=@('MIP processor modification','Graph/MIP API grants/admin consent','New users/licenses','Azure deployment/provisioning','Machine root-store changes','Intake/publish/download enablement');
    mutationsPerformed=$false;authorizationRequired='Separate human approval of this exact web registration/certificate/three user-role assignments before execution'}
$taskPlan | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskRoot 'change-plan.json')
$taskRequest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'app-create.request.json')
$taskDbRoot=Join-Path $taskRoot 'sql'
New-Item -ItemType Directory -Path $taskDbRoot -Force | Out-Null
$taskConfig=[ordered]@{WebIdentity=@{TenantId=$taskTarget.TenantId;ClientId='';CertificateThumbprint=''};
    ConnectionStrings=@{Gateway=('Server=(localdb)\MSSQLLocalDB;Database=GatewayOwnerLocal;AttachDbFilename='+ (Join-Path $taskDbRoot 'GatewayOwnerLocal.mdf')+';Integrated Security=true;Encrypt=false')};
    Portal=@{Identities=@($taskOwner[0],$taskA[0] | ForEach-Object { @{ObjectId=$_.id;DisplayName=$_.userPrincipalName;RmsAddress=$_.userPrincipalName;ReviewReference='Target-bound Graph Member/enabled readback 2026-10-20; directory identity only, not source ownership evidence'} })}}
$taskConfig | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $taskRoot 'web.local.json')
@"
# Local web identity setup proposal

Status: PROPOSAL ONLY; no directory/certificate/resource mutation performed.
Tenant: $($taskTarget.TenantId) / $($taskTarget.VerifiedDomain)
New app: ProtectedExportGateway-Web-Local, single-tenant, separate from MIP processor.
Redirects: https://localhost:7188/auth/callback and https://localhost:7188/auth/signed-out only.
New SP: require user assignment. Two user roles: LibraryReader and DocumentPublisher.
Assignments: Owner gets both; peg-test-a gets LibraryReader only. Exact OIDs/role IDs are in change-plan.json.
Credential: one separate non-exportable 14-day RSA-2048/SHA256 CurrentUser/My test certificate; upload public key only. No client secret or root-store change.
Local HTTPS prerequisite proposed separately: trust only the existing localhost developer certificate $ExistingHttpsThumbprint in CurrentUser/Root after explicit approval. Do not change LocalMachine/Root or export any private key. Rollback removes only this newly added trust entry; preserve the preexisting My certificate/key.
Scopes: openid/profile only. No Graph/MIP API permissions/admin consent. No tenant-wide grants, new users or licenses.
New Azure resource cost: THB 0; existing local Windows/LocalDB. No Azure resource or deployment. Existing licensing remains unverified.
Rollback: stop web host; remove only journaled new assignments/app/SP/exact web certificate and retired private config. Preserve MIP processor, sources and audit evidence. Issued browser sessions/audit may persist.
Stop on target mismatch, name collision, unexpected roles or partial failure; journal exact IDs after each action. No automatic permission widening.
Approval covers the proposed registration, web client certificate, three user-role assignments and exact local HTTPS trust entry; source publishing/downloads remain gated.
"@ | Set-Content -LiteralPath (Join-Path $taskRoot 'change-request.md')
Write-Output 'Prepared exact private web proposal/config. No app registration, role grant, certificate enrollment, DB migration, cloud resource or login performed.'
$global:LASTEXITCODE=0
