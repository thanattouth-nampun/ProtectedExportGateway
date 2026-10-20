param([Parameter(Mandatory)][switch]$ApprovedSetup)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$taskRoot=Join-Path (Split-Path -Parent $PSScriptRoot) '.runtime/owner-web'
$taskJournalPath=Join-Path $taskRoot 'setup-journal.json'
if (Test-Path -LiteralPath $taskJournalPath) {throw 'Existing journal: inspect recorded objects; never repeat creation.'}
$taskPlan=Get-Content -LiteralPath (Join-Path $taskRoot 'change-plan.json') -Raw | ConvertFrom-Json
$taskTenant='9fbc4f5d-56ec-4074-82f0-69d9a86a7c06'
$taskOwner='33f03fd3-d0d1-44c7-a664-9acbe8171560'; $taskA='320cf98c-e4b6-4831-9b40-2aab8dea3d5a'
$taskReader='3c8d970a-c5a4-4e8d-9e87-64c0d9b940bd'; $taskPublisher='32f1c376-2fda-4371-830e-5edb8eea9d71'
$taskHttpsThumb='670DB0B51123B97363776BB7DDD976C7935F799D'
$taskRequest=$taskPlan.appCreateRequest
$taskExpectedAssignments=@("${taskOwner}:$taskReader","${taskOwner}:$taskPublisher","${taskA}:$taskReader")
$taskRedirects=@('https://localhost:7188/auth/callback','https://localhost:7188/auth/signed-out')
if (!$ApprovedSetup -or $taskPlan.tenantId -ne $taskTenant -or $taskRequest.displayName -ne 'ProtectedExportGateway-Web-Local' -or $taskRequest.signInAudience -ne 'AzureADMyOrg' -or @($taskRequest.requiredResourceAccess).Count -ne 0 -or @($taskRequest.appRoles).Count -ne 2 -or @($taskPlan.assignments).Count -ne 3 -or @($taskRequest.web.redirectUris).Count -ne 2 -or (Compare-Object $taskRedirects @($taskRequest.web.redirectUris)) -or $taskRequest.web.implicitGrantSettings.enableAccessTokenIssuance -or $taskRequest.web.implicitGrantSettings.enableIdTokenIssuance -or $taskPlan.localHttpsTrust.existingCertificateThumbprint -ne $taskHttpsThumb) {throw 'Approved payload boundary mismatch.'}
foreach($taskRole in $taskRequest.appRoles) {
    if (!(($taskRole.id -eq $taskReader -and $taskRole.value -eq 'LibraryReader') -or ($taskRole.id -eq $taskPublisher -and $taskRole.value -eq 'DocumentPublisher')) -or !$taskRole.isEnabled -or @($taskRole.allowedMemberTypes).Count -ne 1 -or $taskRole.allowedMemberTypes[0] -ne 'User') {throw 'Unexpected web role.'}
}
if (Compare-Object $taskExpectedAssignments @($taskPlan.assignments | ForEach-Object {"$($_.principalId):$($_.roleId)"})) {throw 'Unexpected assignment.'}
$taskJournal=[ordered]@{status='STARTED';tenantId=$taskTenant;approvalReference='Human 2026-10-20 approved exact web app/certificate/three user roles and current-user localhost trust packet';startedUtc=[DateTime]::UtcNow.ToString('o');appObjectId=$null;clientId=$null;servicePrincipalId=$null;certificate=$null;assignments=@();httpsTrust=[ordered]@{thumbprint=$taskHttpsThumb;store='CurrentUser/Root';preexisting=$null;addedByThisRun=$false};newAzureResources=0}
function Save-TaskJournal {
    $taskJournal | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath ($taskJournalPath+'.new')
    Move-Item -LiteralPath ($taskJournalPath+'.new') -Destination $taskJournalPath -Force
}
$taskHeaders=@{}; $taskAuth=$null
function Invoke-TaskGraph([string]$Method,[string]$Path,$Body=$null) {
    $taskArgs=@{Method=$Method;Uri=('https://graph.microsoft.com/v1.0/'+$Path);Headers=$taskHeaders;ErrorAction='Stop'}
    if ($null -ne $Body) {$taskArgs.ContentType='application/json';$taskArgs.Body=$Body | ConvertTo-Json -Depth 12}
    Invoke-RestMethod @taskArgs
}
try {
    $taskAuth=& az account get-access-token --tenant $taskTenant --resource-type ms-graph --output json --only-show-errors | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $taskAuth.tenant -ne $taskTenant) {throw 'Target authentication unavailable.'}
    $taskHeaders.Authorization='Bearer '+$taskAuth.accessToken
    $taskExisting=Invoke-TaskGraph GET "applications?`$filter=displayName%20eq%20'ProtectedExportGateway-Web-Local'&`$select=id,appId"
    if (@($taskExisting.value).Count -ne 0 -or $null -ne $taskExisting.PSObject.Properties['@odata.nextLink']) {throw 'App collision/incomplete inventory; no reuse.'}
    foreach($taskIdentity in @(@{id=$taskOwner;upn='thanattouth@baht.net'},@{id=$taskA;upn='peg-test-a@baht.net'})) {
        $taskUser=Invoke-TaskGraph GET ("users/"+$taskIdentity.id+"?`$select=id,userPrincipalName,accountEnabled,userType")
        if ($taskUser.userPrincipalName -ne $taskIdentity.upn -or !$taskUser.accountEnabled -or $taskUser.userType -ne 'Member') {throw 'Reviewed identity changed.'}
    }
    $taskHttps=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$taskHttpsThumb)
    if ($taskHttps.Subject -ne 'CN=localhost' -or $taskHttps.NotAfter -le (Get-Date)) {throw 'Local HTTPS certificate changed/expired.'}
    $taskJournal.httpsTrust.preexisting=Test-Path -LiteralPath ('Cert:\CurrentUser\Root\'+$taskHttpsThumb)
    Save-TaskJournal
    $taskCert=New-SelfSignedCertificate -Subject 'CN=ProtectedExportGateway-Web-Local' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddDays(14) -Provider 'Microsoft Software Key Storage Provider'
    $taskKeyId=[guid]::NewGuid().ToString()
    $taskJournal.certificate=[ordered]@{thumbprint=$taskCert.Thumbprint;keyId=$taskKeyId;store='CurrentUser/My';notAfterUtc=$taskCert.NotAfter.ToUniversalTime().ToString('o');privateKeyExported=$false}
    Save-TaskJournal
    $taskCreate=[ordered]@{displayName=$taskRequest.displayName;signInAudience=$taskRequest.signInAudience;requiredResourceAccess=@();web=$taskRequest.web;appRoles=$taskRequest.appRoles;
        keyCredentials=@(@{keyId=$taskKeyId;type='AsymmetricX509Cert';usage='Verify';displayName='14-day dedicated local web client';startDateTime=$taskCert.NotBefore.ToUniversalTime().ToString('o');endDateTime=$taskCert.NotAfter.ToUniversalTime().ToString('o');key=[Convert]::ToBase64String($taskCert.RawData)})}
    $taskApp=Invoke-TaskGraph POST 'applications' $taskCreate
    $taskJournal.appObjectId=$taskApp.id; $taskJournal.clientId=$taskApp.appId; Save-TaskJournal
    $taskSp=Invoke-TaskGraph POST 'servicePrincipals' @{appId=$taskApp.appId;appRoleAssignmentRequired=$true}
    $taskJournal.servicePrincipalId=$taskSp.id; Save-TaskJournal
    foreach($taskAssignment in $taskPlan.assignments) {
        $taskGrant=Invoke-TaskGraph POST ('users/'+$taskAssignment.principalId+'/appRoleAssignments') @{principalId=$taskAssignment.principalId;resourceId=$taskSp.id;appRoleId=$taskAssignment.roleId}
        $taskJournal.assignments += [ordered]@{id=$taskGrant.id;principalId=$taskGrant.principalId;resourceId=$taskGrant.resourceId;appRoleId=$taskGrant.appRoleId;role=$taskAssignment.role}
        Save-TaskJournal
    }
    $taskObservedApp=Invoke-TaskGraph GET ("applications/"+$taskApp.id+"?`$select=id,appId,displayName,signInAudience,requiredResourceAccess,appRoles,keyCredentials,passwordCredentials,web,spa,publicClient")
    $taskObservedSp=Invoke-TaskGraph GET ("servicePrincipals/"+$taskSp.id+"?`$select=id,appId,appRoleAssignmentRequired")
    $taskGrants=Invoke-TaskGraph GET ('servicePrincipals/'+$taskSp.id+'/appRoleAssignedTo')
    if (@($taskGrants.value | Where-Object {$_.resourceId -ne $taskSp.id -or $_.principalType -ne 'User'}).Count) {throw 'Assignment resource/type mismatch.'}
    $taskExpectedRoles=@($taskRequest.appRoles | ForEach-Object {"$($_.id):$($_.value):$($_.isEnabled):$($_.allowedMemberTypes -join ',')"})
    $taskObservedRoles=@($taskObservedApp.appRoles | ForEach-Object {"$($_.id):$($_.value):$($_.isEnabled):$($_.allowedMemberTypes -join ',')"})
    if (Compare-Object $taskExpectedRoles $taskObservedRoles) {throw 'Exact role readback mismatch.'}
    if ($taskObservedApp.web.implicitGrantSettings.enableAccessTokenIssuance -or $taskObservedApp.web.implicitGrantSettings.enableIdTokenIssuance) {throw 'Implicit grant unexpectedly enabled.'}
    if (@($taskGrants.value).Count -ne 3 -or $null -ne $taskGrants.PSObject.Properties['@odata.nextLink'] -or (Compare-Object $taskExpectedAssignments @($taskGrants.value | ForEach-Object {"$($_.principalId):$($_.appRoleId)"}))) {throw 'Assignment readback mismatch.'}
    if ($taskObservedApp.signInAudience -ne 'AzureADMyOrg' -or @($taskObservedApp.requiredResourceAccess).Count -ne 0 -or @($taskObservedApp.passwordCredentials).Count -ne 0 -or @($taskObservedApp.keyCredentials).Count -ne 1 -or $taskObservedApp.keyCredentials[0].keyId -ne $taskKeyId -or !$taskObservedSp.appRoleAssignmentRequired -or @($taskObservedApp.web.redirectUris).Count -ne 2 -or (Compare-Object $taskRedirects @($taskObservedApp.web.redirectUris)) -or @($taskObservedApp.spa.redirectUris).Count -ne 0 -or @($taskObservedApp.publicClient.redirectUris).Count -ne 0 -or @($taskObservedApp.appRoles).Count -ne 2) {throw 'Application readback mismatch.'}
    $taskObservedApp | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $taskRoot 'app-readback.json')
    $taskGrants.value | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskRoot 'assignments-readback.json')
    if (!$taskJournal.httpsTrust.preexisting) {
        # Construct a public-only certificate object; never export or re-enroll the existing private key.
        $taskPublic=[Security.Cryptography.X509Certificates.X509Certificate2]::new($taskHttps.RawData)
        $taskTrust=[Security.Cryptography.X509Certificates.X509Store]::new('Root','CurrentUser')
        try {$taskTrust.Open('ReadWrite');$taskTrust.Add($taskPublic)} finally {$taskTrust.Dispose();$taskPublic.Dispose()}
        $taskJournal.httpsTrust.addedByThisRun=$true; Save-TaskJournal
    }
    if (!(Test-Path -LiteralPath ('Cert:\CurrentUser\Root\'+$taskHttpsThumb))) {throw 'Exact HTTPS trust readback failed.'}
    $taskConfigPath=Join-Path $taskRoot 'web.local.json'
    $taskConfig=Get-Content -LiteralPath $taskConfigPath -Raw | ConvertFrom-Json
    if ($taskConfig.WebIdentity.TenantId -ne $taskTenant) {throw 'Local config target mismatch.'}
    $taskConfig.WebIdentity.ClientId=$taskApp.appId; $taskConfig.WebIdentity.CertificateThumbprint=$taskCert.Thumbprint
    $taskConfig | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskConfigPath
    $taskJournal.status='VERIFIED_SETUP';$taskJournal['completedUtc']=[DateTime]::UtcNow.ToString('o');Save-TaskJournal
    Write-Output ('Verified web app '+$taskApp.appId+'; three exact user assignments, separate certificate and exact CurrentUser HTTPS trust. Login not yet tested.')
} catch {
    $taskJournal.status='STOPPED_REVIEW_REQUIRED';$taskJournal['failureCategory']=$_.Exception.GetType().Name;$taskJournal['failureLine']=$_.InvocationInfo.ScriptLineNumber;Save-TaskJournal
    throw ('Web setup stopped; inspect exact private journal. Category: '+$_.Exception.GetType().Name)
} finally {$taskHeaders.Clear();if ($null -ne $taskAuth) {$taskAuth.accessToken=$null};$taskAuth=$null}
