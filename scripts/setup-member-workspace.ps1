param([Parameter(Mandatory)][switch]$ApprovedSetup)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$taskRoot=Join-Path (Split-Path -Parent $PSScriptRoot) '.runtime/member-workspace'
$taskJournalPath=Join-Path $taskRoot 'execution-journal.json'
if (!$ApprovedSetup -or (Test-Path -LiteralPath $taskJournalPath)) {throw 'Explicit approved setup and no existing execution journal required; never repeat mutations.'}
$taskPlan=Get-Content (Join-Path $taskRoot 'change-plan.json') -Raw | ConvertFrom-Json
$taskTenant='9fbc4f5d-56ec-4074-82f0-69d9a86a7c06'
$taskApp='a490d041-acd3-43a3-89c2-94820c6dabae'
$taskClient='a834c776-387c-43aa-8d55-3ab62a08acf6'
$taskSp='3cd6f447-4747-403a-8741-a034f3870e9c'
if ($taskPlan.tenantId -ne $taskTenant -or $taskPlan.appObjectId -ne $taskApp -or $taskPlan.clientId -ne $taskClient -or $taskPlan.servicePrincipalId -ne $taskSp) {throw 'Approved target mismatch.'}
$taskHeaders=@{};$taskAuth=$null;$taskJournal=$null
function Invoke-MemberGraph([string]$Method,[string]$Path,$Body=$null) {
    $taskArgs=@{Method=$Method;Uri=('https://graph.microsoft.com/v1.0/'+$Path);Headers=$taskHeaders;ErrorAction='Stop'}
    if ($null -ne $Body) {$taskArgs.ContentType='application/json';$taskArgs.Body=$Body | ConvertTo-Json -Depth 8}
    Invoke-RestMethod @taskArgs
}
function Save-MemberJournal {
    $taskJournal | ConvertTo-Json -Depth 12 | Set-Content ($taskJournalPath+'.new')
    Move-Item -LiteralPath ($taskJournalPath+'.new') -Destination $taskJournalPath -Force
}
function AssignmentTuples($items) {@($items | ForEach-Object {"$($_.id):$($_.principalId):$($_.resourceId):$($_.appRoleId)"})}
function ConsentTuples($items) {@($items | ForEach-Object {"$($_.id):$($_.clientId):$($_.principalId):$($_.resourceId):$($_.consentType):$($_.scope)"})}
try {
    $taskAuth=& az account get-access-token --tenant $taskTenant --resource-type ms-graph --output json --only-show-errors | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $taskAuth.tenant -ne $taskTenant) {throw 'Target auth unavailable.'}
    $taskHeaders.Authorization='Bearer '+$taskAuth.accessToken
    $taskBeforeApp=Invoke-MemberGraph GET ("applications/$taskApp`?`$select=id,appId,optionalClaims,signInAudience,requiredResourceAccess,appRoles,web,keyCredentials,passwordCredentials")
    $taskBeforeSp=Invoke-MemberGraph GET ("servicePrincipals/$taskSp`?`$select=id,appId,appRoleAssignmentRequired")
    $taskBeforeRoles=Invoke-MemberGraph GET ("servicePrincipals/$taskSp/appRoleAssignedTo")
    $taskBeforeConsents=Invoke-MemberGraph GET ("servicePrincipals/$taskSp/oauth2PermissionGrants")
    if ($taskBeforeApp.appId -ne $taskClient -or $taskBeforeSp.appId -ne $taskClient -or $taskBeforeApp.signInAudience -ne 'AzureADMyOrg' -or $null -ne $taskBeforeApp.optionalClaims -or !$taskBeforeSp.appRoleAssignmentRequired) {throw 'Prestate changed; review without mutation.'}
    if ($null -ne $taskBeforeRoles.PSObject.Properties['@odata.nextLink'] -or $null -ne $taskBeforeConsents.PSObject.Properties['@odata.nextLink']) {throw 'Incomplete prestate inventory.'}
    $taskJournal=[ordered]@{status='STARTED';tenantId=$taskTenant;appObjectId=$taskApp;clientId=$taskClient;servicePrincipalId=$taskSp;approvalReference='Human approved exact acct ID-token claim and assignment-required true-to-false after local 183-test checkpoint';startedUtc=[DateTime]::UtcNow.ToString('o');before=@{optionalClaims=$null;appRoleAssignmentRequired=$true};claimPatchAttempted=$false;claimPatchAcknowledged=$false;assignmentPatchAttempted=$false;assignmentPatchAcknowledged=$false}
    $taskBeforeApp | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $taskRoot 'app-before.json')
    $taskBeforeRoles | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $taskRoot 'assignments-before.json')
    $taskBeforeConsents | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $taskRoot 'consents-before.json')
    Save-MemberJournal
    $taskJournal.claimPatchAttempted=$true;Save-MemberJournal
    $null=Invoke-MemberGraph PATCH ("applications/$taskApp") @{optionalClaims=@{idToken=@(@{name='acct';source=$null;essential=$false;additionalProperties=@()});accessToken=@();saml2Token=@()}}
    $taskJournal.claimPatchAcknowledged=$true;Save-MemberJournal
    $taskJournal.assignmentPatchAttempted=$true;Save-MemberJournal
    $null=Invoke-MemberGraph PATCH ("servicePrincipals/$taskSp") @{appRoleAssignmentRequired=$false}
    $taskJournal.assignmentPatchAcknowledged=$true;Save-MemberJournal
    $taskAfterApp=Invoke-MemberGraph GET ("applications/$taskApp`?`$select=id,appId,optionalClaims,signInAudience,requiredResourceAccess,appRoles,web,keyCredentials,passwordCredentials")
    $taskAfterSp=Invoke-MemberGraph GET ("servicePrincipals/$taskSp`?`$select=id,appId,appRoleAssignmentRequired")
    $taskAfterRoles=Invoke-MemberGraph GET ("servicePrincipals/$taskSp/appRoleAssignedTo")
    $taskAfterConsents=Invoke-MemberGraph GET ("servicePrincipals/$taskSp/oauth2PermissionGrants")
    if ($taskAfterSp.appRoleAssignmentRequired -or $taskAfterApp.appId -ne $taskClient -or $taskAfterSp.appId -ne $taskClient) {throw 'Assignment target readback mismatch.'}
    $taskClaims=$taskAfterApp.optionalClaims
    if (@($taskClaims.idToken).Count -ne 1 -or $taskClaims.idToken[0].name -cne 'acct' -or $null -ne $taskClaims.idToken[0].source -or $taskClaims.idToken[0].essential -or @($taskClaims.idToken[0].additionalProperties).Count -ne 0 -or @($taskClaims.accessToken).Count -ne 0 -or @($taskClaims.saml2Token).Count -ne 0) {throw 'Exact acct claim readback mismatch.'}
    foreach($taskField in @('signInAudience','requiredResourceAccess','appRoles','web','keyCredentials','passwordCredentials')) {
        if (($taskBeforeApp.$taskField | ConvertTo-Json -Depth 12 -Compress) -cne ($taskAfterApp.$taskField | ConvertTo-Json -Depth 12 -Compress)) {throw 'Unrelated app configuration changed.'}
    }
    if ($null -ne $taskAfterRoles.PSObject.Properties['@odata.nextLink'] -or $null -ne $taskAfterConsents.PSObject.Properties['@odata.nextLink']) {throw 'Incomplete readback inventory.'}
    if ((Compare-Object (AssignmentTuples $taskBeforeRoles.value) (AssignmentTuples $taskAfterRoles.value)) -or (Compare-Object (ConsentTuples $taskBeforeConsents.value) (ConsentTuples $taskAfterConsents.value))) {throw 'Existing roles/consent changed.'}
    $taskAfterApp | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $taskRoot 'app-after.json')
    $taskJournal.status='VERIFIED';$taskJournal['rolesAndConsentUnchanged']=$true;$taskJournal['unrelatedAppConfigUnchanged']=$true;$taskJournal['completedUtc']=[DateTime]::UtcNow.ToString('o');Save-MemberJournal
    Write-Output 'Verified exact acct ID-token claim and assignment-required=false; app roles, consent and unrelated configuration unchanged. Fresh login still required.'
} catch {
    if ($null -ne $taskJournal) {$taskJournal.status='STOPPED_REVIEW_REQUIRED';$taskJournal['failureCategory']=$_.Exception.GetType().Name;$taskJournal['failureLine']=$_.InvocationInfo.ScriptLineNumber;Save-MemberJournal}
    throw ('Member setup stopped; inspect journal/readback before any retry or rollback. Category: '+$_.Exception.GetType().Name)
} finally {$taskHeaders.Clear();if ($null -ne $taskAuth) {$taskAuth.accessToken=$null};$taskAuth=$null}
