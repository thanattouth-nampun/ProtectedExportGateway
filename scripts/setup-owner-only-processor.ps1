param([Parameter(Mandatory)][switch]$ApprovedSetupOnly,[switch]$ResumeRecordedSetup)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRoot = Join-Path (Split-Path -Parent $PSScriptRoot) '.runtime/owner-only-inventory'
$taskJournalPath = Join-Path $taskRoot 'setup-journal.json'
if ((Test-Path -LiteralPath $taskJournalPath) -and !$ResumeRecordedSetup) { throw 'Setup journal already exists. Inspect it; do not repeat creation.' }
$taskPlan = Get-Content -LiteralPath (Join-Path $taskRoot 'change-plan.json') -Raw | ConvertFrom-Json
$taskTenant = '9fbc4f5d-56ec-4074-82f0-69d9a86a7c06'
if (!$ApprovedSetupOnly -or $taskPlan.tenantId -ne $taskTenant) { throw 'Approved target setup required.' }
$taskExpected = @{
    '7f740376-647b-4ad7-9ff7-292af252707a' = '00000012-0000-0000-c000-000000000000'
    'd13f921c-7f21-4c08-bade-db9d048bd0da' = '00000012-0000-0000-c000-000000000000'
    '8b2071cd-015a-4025-8052-1c0dba2d3f64' = '870c4f2e-85b6-4d43-bdda-6ed9a579b725'
}
$taskRequest = $taskPlan.appCreateRequest
if ($taskRequest.displayName -ne 'ProtectedExportGateway-MipSpike' -or $taskRequest.signInAudience -ne 'AzureADMyOrg' -or @($taskRequest.requiredResourceAccess).Count -ne 2) { throw 'Unexpected app payload.' }
$taskSeen = @()
foreach ($taskResource in $taskRequest.requiredResourceAccess) {
    foreach ($taskRole in $taskResource.resourceAccess) {
        if (!$taskExpected.ContainsKey($taskRole.id) -or $taskExpected[$taskRole.id] -ne $taskResource.resourceAppId -or $taskRole.type -ne 'Role') { throw 'Unexpected permission payload.' }
        $taskSeen += $taskRole.id
    }
}
if ($taskSeen.Count -ne 3 -or @($taskSeen | Select-Object -Unique).Count -ne 3 -or @($taskPlan.applicationPermissions).Count -ne 3) { throw 'Expected three unique roles.' }
$taskJournal = [ordered]@{status='STARTED';tenantId=$taskTenant;approvedScope='Human approved named processor, 14-day certificate and three exact application roles; setup only';startedUtc=[DateTime]::UtcNow.ToString('o');appObjectId=$null;clientId=$null;servicePrincipalId=$null;certificate=$null;assignments=@();liveMipRun=$false;azureResourcesCreated=0}
if ($ResumeRecordedSetup) {
    $taskJournal = Get-Content -LiteralPath $taskJournalPath -Raw | ConvertFrom-Json -AsHashtable
    if ($taskJournal.tenantId -ne $taskTenant -or $taskJournal.status -ne 'STOPPED_REVIEW_REQUIRED' -or !$taskJournal.appObjectId -or !$taskJournal.servicePrincipalId -or !$taskJournal.certificate) { throw 'Resume requires a reviewed stopped journal with recorded app, SP and certificate.' }
}
function Save-TaskJournal {
    $taskTempJournal = $taskJournalPath+'.new'
    $taskJournal | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $taskTempJournal
    Move-Item -LiteralPath $taskTempJournal -Destination $taskJournalPath -Force
}
function Invoke-TaskGraph([string]$Method,[string]$Path,$Body=$null) {
    $taskArgs = @{Method=$Method;Uri=('https://graph.microsoft.com/v1.0/'+$Path);Headers=$taskHeaders;ErrorAction='Stop'}
    if ($null -ne $Body) { $taskArgs.Body=$Body | ConvertTo-Json -Depth 12; $taskArgs.ContentType='application/json' }
    Invoke-RestMethod @taskArgs
}
$taskHeaders = @{}
$taskAuth = $null
try {
    $taskAuth = & az account get-access-token --tenant $taskTenant --resource-type ms-graph --output json --only-show-errors | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $taskAuth.tenant -ne $taskTenant) { throw 'Target token unavailable.' }
    $taskHeaders.Authorization = 'Bearer '+$taskAuth.accessToken
    $taskExisting = Invoke-TaskGraph GET "applications?`$filter=displayName%20eq%20'ProtectedExportGateway-MipSpike'&`$select=id,appId,displayName"
    if ($null -ne $taskExisting.PSObject.Properties['@odata.nextLink']) { throw 'Incomplete inventory.' }
    if (!$ResumeRecordedSetup -and @($taskExisting.value).Count -ne 0) { throw 'Name collision. Stop without changes.' }
    if ($ResumeRecordedSetup -and (@($taskExisting.value).Count -ne 1 -or $taskExisting.value[0].id -ne $taskJournal.appObjectId -or $taskExisting.value[0].appId -ne $taskJournal.clientId)) { throw 'Recorded app differs from exact live lookup.' }
    foreach ($taskPermission in $taskPlan.applicationPermissions) {
        if (!$taskExpected.ContainsKey($taskPermission.roleId) -or $taskExpected[$taskPermission.roleId] -ne $taskPermission.resourceAppId) { throw 'Assignment allowlist mismatch.' }
        $taskObserved = Invoke-TaskGraph GET ("servicePrincipals/"+$taskPermission.resourceObjectId+"?`$select=id,appId,appRoles")
        $taskObservedRole = @($taskObserved.appRoles | Where-Object { $_.id -eq $taskPermission.roleId -and $_.value -eq $taskPermission.role -and $_.isEnabled -and $_.allowedMemberTypes -contains 'Application' })
        if ($taskObserved.appId -ne $taskPermission.resourceAppId -or $taskObservedRole.Count -ne 1) { throw 'Live resource role validation failed.' }
    }
    if (!$ResumeRecordedSetup) {
    Save-TaskJournal
    $taskCert = New-SelfSignedCertificate -Subject 'CN=ProtectedExportGateway-MipSpike' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddDays(14) -Provider 'Microsoft Software Key Storage Provider'
    $taskKeyId = [guid]::NewGuid().ToString()
    $taskJournal.certificate = [ordered]@{thumbprint=$taskCert.Thumbprint;keyId=$taskKeyId;store='CurrentUser/My';notBeforeUtc=$taskCert.NotBefore.ToUniversalTime().ToString('o');notAfterUtc=$taskCert.NotAfter.ToUniversalTime().ToString('o');privateKeyExported=$false}
    Save-TaskJournal
    $taskCreate = [ordered]@{displayName=$taskRequest.displayName;signInAudience=$taskRequest.signInAudience;requiredResourceAccess=$taskRequest.requiredResourceAccess;keyCredentials=@(@{keyId=$taskKeyId;type='AsymmetricX509Cert';usage='Verify';displayName='14-day local feasibility certificate';startDateTime=$taskCert.NotBefore.ToUniversalTime().ToString('o');endDateTime=$taskCert.NotAfter.ToUniversalTime().ToString('o');key=[Convert]::ToBase64String($taskCert.RawData)})}
    $taskApp = Invoke-TaskGraph POST 'applications' $taskCreate
    $taskJournal.appObjectId=$taskApp.id; $taskJournal.clientId=$taskApp.appId
    Save-TaskJournal
    $taskSp = Invoke-TaskGraph POST 'servicePrincipals' @{appId=$taskApp.appId}
    $taskJournal.servicePrincipalId=$taskSp.id
    Save-TaskJournal
    } else {
        $taskApp = Invoke-TaskGraph GET ('applications/'+$taskJournal.appObjectId+'?'+ '$select=id,appId,keyCredentials,passwordCredentials')
        $taskSp = Invoke-TaskGraph GET ('servicePrincipals/'+$taskJournal.servicePrincipalId+'?'+ '$select=id,appId')
        $taskKeyId = $taskJournal.certificate.keyId
        $taskCert = Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$taskJournal.certificate.thumbprint)
        if ($taskApp.appId -ne $taskJournal.clientId -or $taskSp.appId -ne $taskJournal.clientId -or @($taskApp.keyCredentials).Count -ne 1 -or $taskApp.keyCredentials[0].keyId -ne $taskKeyId -or @($taskApp.passwordCredentials).Count -ne 0 -or !$taskCert.HasPrivateKey) { throw 'Recorded identity/certificate verification failed.' }
    }
    $taskBeforeGrants = Invoke-TaskGraph GET ("servicePrincipals/"+$taskSp.id+'/appRoleAssignments')
    if ($null -ne $taskBeforeGrants.PSObject.Properties['@odata.nextLink']) { throw 'Incomplete assignment inventory.' }
    foreach ($taskGrant in $taskBeforeGrants.value) {
        if (@($taskPlan.applicationPermissions | Where-Object { $_.roleId -eq $taskGrant.appRoleId -and $_.resourceObjectId -eq $taskGrant.resourceId }).Count -ne 1) { throw 'Unexpected existing assignment.' }
    }
    foreach ($taskPermission in $taskPlan.applicationPermissions) {
        $taskExistingGrant = @($taskBeforeGrants.value | Where-Object { $_.appRoleId -eq $taskPermission.roleId -and $_.resourceId -eq $taskPermission.resourceObjectId })
        if ($taskExistingGrant.Count -gt 1) { throw 'Duplicate existing assignment.' }
        if ($taskExistingGrant.Count -eq 1) {
            if (@($taskJournal.assignments | Where-Object { $_.id -eq $taskExistingGrant[0].id }).Count -eq 0) {
                $taskJournal.assignments += [ordered]@{id=$taskExistingGrant[0].id;resourceId=$taskExistingGrant[0].resourceId;appRoleId=$taskExistingGrant[0].appRoleId;role=$taskPermission.role}
                Save-TaskJournal
            }
            continue
        }
        $taskAssignment = Invoke-TaskGraph POST ("servicePrincipals/"+$taskSp.id+'/appRoleAssignments') @{principalId=$taskSp.id;resourceId=$taskPermission.resourceObjectId;appRoleId=$taskPermission.roleId}
        $taskJournal.assignments += [ordered]@{id=$taskAssignment.id;resourceId=$taskAssignment.resourceId;appRoleId=$taskAssignment.appRoleId;role=$taskPermission.role}
        Save-TaskJournal
    }
    $taskReadback = Invoke-TaskGraph GET ("applications/"+$taskApp.id+"?`$select=id,appId,displayName,signInAudience,requiredResourceAccess,keyCredentials,passwordCredentials,web,spa,publicClient")
    $taskGrants = Invoke-TaskGraph GET ("servicePrincipals/"+$taskSp.id+'/appRoleAssignments')
    if (@($taskGrants.value).Count -ne 3 -or $null -ne $taskGrants.PSObject.Properties['@odata.nextLink']) { throw 'Unexpected role assignment count.' }
    foreach ($taskGrant in $taskGrants.value) {
        if (@($taskPlan.applicationPermissions | Where-Object { $_.roleId -eq $taskGrant.appRoleId -and $_.resourceObjectId -eq $taskGrant.resourceId }).Count -ne 1 -or $taskGrant.principalId -ne $taskSp.id) { throw 'Unexpected role assignment.' }
    }
    $taskReadRoles=@($taskReadback.requiredResourceAccess | ForEach-Object { $taskResourceId=$_.resourceAppId; $_.resourceAccess | ForEach-Object { if (!$taskExpected.ContainsKey($_.id) -or $taskExpected[$_.id] -ne $taskResourceId -or $_.type -ne 'Role') { throw 'Unexpected registered permission.' }; $_.id } })
    if ($taskReadback.signInAudience -ne 'AzureADMyOrg' -or $taskReadRoles.Count -ne 3 -or @($taskReadRoles | Select-Object -Unique).Count -ne 3 -or @($taskReadback.keyCredentials).Count -ne 1 -or $taskReadback.keyCredentials[0].keyId -ne $taskKeyId -or @($taskReadback.passwordCredentials).Count -ne 0 -or @($taskReadback.web.redirectUris).Count -ne 0 -or @($taskReadback.spa.redirectUris).Count -ne 0 -or @($taskReadback.publicClient.redirectUris).Count -ne 0) { throw 'Application verification failed.' }
    $taskReadback | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $taskRoot 'setup-app-readback.json')
    $taskGrants.value | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'setup-grants-readback.json')
    $taskJournal.status='VERIFIED_SETUP_ONLY'; $taskJournal.completedUtc=[DateTime]::UtcNow.ToString('o')
    Save-TaskJournal
    Write-Output ('Verified setup only. Client ID: '+$taskApp.appId+'; exactly three roles; no live MIP processing or Azure provisioning.')
} catch {
    $taskJournal.status='STOPPED_REVIEW_REQUIRED'
    $taskJournal['failureCategory']=$_.Exception.GetType().Name
    $taskJournal['failureLine']=$_.InvocationInfo.ScriptLineNumber
    Save-TaskJournal
    throw ('Setup stopped; inspect private journal. Error category: '+$_.Exception.GetType().Name)
} finally {
    $taskHeaders.Clear()
    if ($null -ne $taskAuth) { $taskAuth.accessToken=$null }
    $taskAuth=$null
}
