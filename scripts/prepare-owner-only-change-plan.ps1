$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$taskRoot = Join-Path $repoRoot '.runtime/owner-only-inventory'
if (Test-Path -LiteralPath (Join-Path $taskRoot 'setup-journal.json')) { throw 'A setup journal exists. Preserve executed scope and inspect it before preparing another proposal.' }
$taskInventory = Get-Content -LiteralPath (Join-Path $taskRoot 'directory-metadata.json') -Raw | ConvertFrom-Json
if ($taskInventory.domainHttpStatus -ne 200 -or $taskInventory.resourceHttpStatus -ne 200) { throw 'Successful target metadata inspection is required.' }
$taskTarget = Get-Content -LiteralPath (Join-Path $repoRoot '.runtime/owner-only-target.json') -Raw | ConvertFrom-Json
if ($taskTarget.TenantId -ne $taskInventory.tenantId) { throw 'Inventory tenant mismatch.' }
$taskDefault = @($taskInventory.domains | Where-Object { $_.isVerified -and $_.isDefault })
if ($taskDefault.Count -ne 1) { throw 'One verified default domain is required.' }
$taskRequired = @('Content.DelegatedReader','Content.DelegatedWriter','UnifiedPolicy.Tenant.Read')
$taskApprovedResources = @{
    'Content.DelegatedReader' = @{appId='00000012-0000-0000-c000-000000000000';roleId='7f740376-647b-4ad7-9ff7-292af252707a'}
    'Content.DelegatedWriter' = @{appId='00000012-0000-0000-c000-000000000000';roleId='d13f921c-7f21-4c08-bade-db9d048bd0da'}
    'UnifiedPolicy.Tenant.Read' = @{appId='870c4f2e-85b6-4d43-bdda-6ed9a579b725';roleId='8b2071cd-015a-4025-8052-1c0dba2d3f64'}
}
$taskPermissions = @()
foreach ($taskValue in $taskRequired) {
    $taskMatches = @($taskInventory.resourceMetadata | Where-Object { @($_.relevantApplicationRoles | ForEach-Object {$_.value}) -contains $taskValue })
    if ($taskMatches.Count -ne 1) { throw "Unambiguous resource required for $taskValue" }
    $taskResource = $taskMatches[0]
    $taskRole = @($taskResource.relevantApplicationRoles | Where-Object value -eq $taskValue)
    if ($taskRole.Count -ne 1) { throw 'Unambiguous application role required.' }
    if ($taskResource.appId -ne $taskApprovedResources[$taskValue].appId -or $taskRole[0].id -ne $taskApprovedResources[$taskValue].roleId) { throw 'Observed resource/role differs from the reviewed allowlist.' }
    $taskPermissions += [pscustomobject][ordered]@{ resourceAppId=$taskResource.appId;resourceObjectId=$taskResource.id;resourceName=$taskResource.displayName;roleId=$taskRole[0].id;role=$taskValue;type='Role' }
}
$taskRequest = [ordered]@{
    displayName='ProtectedExportGateway-MipSpike'
    signInAudience='AzureADMyOrg'
    requiredResourceAccess=@($taskPermissions | Group-Object resourceAppId | ForEach-Object {
        [ordered]@{resourceAppId=$_.Name;resourceAccess=@($_.Group | ForEach-Object { [ordered]@{id=$_.roleId;type='Role'} })}
    })
}
$taskResources = @($taskRequest.requiredResourceAccess)
if ($taskResources.Count -ne 2 -or @($taskResources | Where-Object { [string]::IsNullOrWhiteSpace($_.resourceAppId) }).Count -ne 0) { throw 'Expected exactly two nonempty resource application IDs.' }
foreach ($taskPermission in $taskPermissions) {
    $taskGroup = @($taskResources | Where-Object { $_.resourceAppId -eq $taskPermission.resourceAppId })
    if ($taskGroup.Count -ne 1 -or @($taskGroup[0].resourceAccess | Where-Object { $_.id -eq $taskPermission.roleId -and $_.type -eq 'Role' }).Count -ne 1) { throw 'Required resource payload does not match the exact permission allowlist.' }
}
if (@($taskResources | ForEach-Object { $_.resourceAccess }).Count -ne 3) { throw 'Unexpected permission count.' }
$taskPlan = [ordered]@{
    status='PROPOSAL_NOT_APPROVED';tenantId=$taskInventory.tenantId;verifiedDefaultDomain=$taskDefault[0].id
    appCreateRequest=$taskRequest;servicePrincipal='Create only for the newly returned application client ID'
    certificate=[ordered]@{store='CurrentUser/My';algorithm='RSA';keyBits=2048;hash='SHA256';keyExportPolicy='NonExportable';validDays=14;trustRootChanges=$false;passwordSecret=$false;publicKeyUploadOnly=$true}
    applicationPermissions=$taskPermissions;permissionBoundary='Application permissions support acting on behalf of users; owner allowlist is enforced by service code, not a per-owner consent boundary'
    excludedPermissions=@('Content.SuperUser','Content.Writer','Microsoft Graph file/directory permissions')
    scope='Dedicated local feasibility processor setup only; no MIP source processing until separate fixture/owner/label scope is ready'
    azureResourcesCreated=0;estimatedNewAzureResourceCostThb=0;costAssumptions='Existing Windows host and existing licensed test identities only; no new accounts, license purchase, Azure VM/storage/SQL or deployment. M365 entitlement not verified.'
    rollback=@('Record newly created app object/client ID, service principal ID, certificate key ID/thumbprint and each assignment ID before further changes',
        'Remove only the three explicitly recorded app-role assignments; revoke/delete only the new processor identity and app objects',
        'Remove only the recorded local certificate/private-key material after dependent processes stop; never alter trusted roots',
        'Remove only exact private run paths after verified cleanup; audit records and issued token/license caches may persist')
    stopConditions=@('Target tenant mismatch','App name already exists: do not overwrite or reuse silently','Unexpected resource/role or additional permission','Insufficient administrator authorization','Any partial failure: stop and preserve exact object journal')
    mutationsPerformed=$false;adminConsentPerformed=$false;liveMipRunApproved=$false
}
if ($null -ne $taskTarget.PSObject.Properties['Subscription']) {
    if ($taskTarget.Subscription.tenantId -ne $taskInventory.tenantId -or $taskTarget.Subscription.state -ne 'Enabled') { throw 'Selected subscription is not enabled in the target tenant.' }
    $taskPlan['futureSubscriptionTarget'] = $taskTarget.Subscription
}
$taskRequest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'app-create.request.json')
$taskPlan | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $taskRoot 'change-plan.json')
$taskTarget.TenantIdStatus = 'AUTHENTICATED_GRAPH_METADATA_VERIFIED'
$taskTarget.VerifiedDomain = $taskDefault[0].id
$taskTarget | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repoRoot '.runtime/owner-only-target.json')
Write-Output 'Prepared local proposal and exact application permission payload. No tenant mutation or certificate enrollment performed.'
$global:LASTEXITCODE = 0
