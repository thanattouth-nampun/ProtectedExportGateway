param([switch]$ApprovedConsent)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Default mode is read-only against Entra; writes a private review packet locally.
# Mutation requires the human's separate approval of that packet, then this switch.
$taskRoot=Join-Path (Split-Path -Parent $PSScriptRoot) '.runtime/owner-web/reader-consent'
New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
$taskJournalPath=Join-Path $taskRoot 'execution-journal.json'
if ($ApprovedConsent -and (Test-Path -LiteralPath $taskJournalPath)) {throw 'Execution journal exists: inspect exact recorded grant; never repeat creation.'}
$taskTenant='9fbc4f5d-56ec-4074-82f0-69d9a86a7c06'
$taskClient='3cd6f447-4747-403a-8741-a034f3870e9c'
$taskClientApp='a834c776-387c-43aa-8d55-3ab62a08acf6'
$taskUser='320cf98c-e4b6-4831-9b40-2aab8dea3d5a'
$taskResource='f8faa1d5-b6b1-4588-82ea-d90abbf28e4c'
$taskReaderRole='3c8d970a-c5a4-4e8d-9e87-64c0d9b940bd'
$taskHeaders=@{};$taskAuth=$null;$taskJournal=$null
function Invoke-ReaderGraph([string]$Method,[string]$Path,$Body=$null) {
    $taskArgs=@{Method=$Method;Uri=('https://graph.microsoft.com/v1.0/'+$Path);Headers=$taskHeaders;ErrorAction='Stop'}
    if ($null -ne $Body) {$taskArgs.ContentType='application/json';$taskArgs.Body=$Body | ConvertTo-Json -Depth 5}
    Invoke-RestMethod @taskArgs
}
function Save-ReaderJournal {
    $taskJournal | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath ($taskJournalPath+'.new')
    Move-Item -LiteralPath ($taskJournalPath+'.new') -Destination $taskJournalPath -Force
}
try {
    $taskAuth=& az account get-access-token --tenant $taskTenant --resource-type ms-graph --output json --only-show-errors | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $taskAuth.tenant -ne $taskTenant) {throw 'Target authentication unavailable.'}
    $taskHeaders.Authorization='Bearer '+$taskAuth.accessToken
    $taskSp=Invoke-ReaderGraph GET ("servicePrincipals/$taskClient`?`$select=id,appId,appRoleAssignmentRequired")
    $taskResourceSp=Invoke-ReaderGraph GET ("servicePrincipals/$taskResource`?`$select=id,appId,oauth2PermissionScopes")
    $taskIdentity=Invoke-ReaderGraph GET ("users/$taskUser`?`$select=id,userPrincipalName,accountEnabled,userType")
    if ($taskSp.appId -ne $taskClientApp -or !$taskSp.appRoleAssignmentRequired) {throw 'Web identity/assignment boundary changed.'}
    if ($taskResourceSp.appId -ne '00000003-0000-0000-c000-000000000000') {throw 'OIDC resource changed.'}
    if ($taskIdentity.userPrincipalName -ne 'peg-test-a@baht.net' -or !$taskIdentity.accountEnabled -or $taskIdentity.userType -ne 'Member') {throw 'Reviewed A identity changed.'}
    foreach ($taskScope in @('openid','profile')) {
        if (@($taskResourceSp.oauth2PermissionScopes | Where-Object {$_.value -eq $taskScope -and $_.isEnabled}).Count -ne 1) {throw 'OIDC scope unavailable.'}
    }
    $taskAssignments=Invoke-ReaderGraph GET ("servicePrincipals/$taskClient/appRoleAssignedTo")
    if ($null -ne $taskAssignments.PSObject.Properties['@odata.nextLink']) {throw 'Assignment inventory incomplete.'}
    $taskAGrants=@($taskAssignments.value | Where-Object {$_.principalId -eq $taskUser})
    if ($taskAGrants.Count -ne 1 -or $taskAGrants[0].appRoleId -ne $taskReaderRole -or $taskAGrants[0].resourceId -ne $taskClient -or $taskAGrants[0].principalType -ne 'User') {throw 'A must have only the reviewed Reader assignment.'}
    $taskExisting=Invoke-ReaderGraph GET ("servicePrincipals/$taskClient/oauth2PermissionGrants")
    if ($null -ne $taskExisting.PSObject.Properties['@odata.nextLink']) {throw 'Consent inventory incomplete.'}
    if (@($taskExisting.value | Where-Object {$_.consentType -eq 'AllPrincipals' -or $_.principalId -eq $taskUser}).Count) {throw 'Applicable consent already exists; inspect it without creating or expanding grants.'}
    $taskPayload=[ordered]@{clientId=$taskClient;consentType='Principal';principalId=$taskUser;resourceId=$taskResource;scope='openid profile'}
    if (!$ApprovedConsent) {
        $taskPayload | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'create.request.json')
        [ordered]@{status='PREPARED_NOT_APPROVED';tenantId=$taskTenant;webAppId=$taskClientApp;observedUtc=[DateTime]::UtcNow.ToString('o');request=$taskPayload;existingGrantCount=@($taskExisting.value).Count;newAzureResources=0;estimatedAdditionalResourceCostThb=0;rollback='Delete only the newly created oauth2PermissionGrant ID recorded in execution-journal.json; preserve existing Owner consent, app roles and tenant consent policy. Issued tokens/sessions can remain until expiry.'} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'change-plan.json')
        Write-Output 'Prepared A-only openid/profile consent request. No Entra mutation performed.'
        return
    }
    $taskPlanPath=Join-Path $taskRoot 'change-plan.json'
    if (!(Test-Path -LiteralPath $taskPlanPath)) {throw 'Prepare and review the exact packet first.'}
    $taskPlan=Get-Content -LiteralPath $taskPlanPath -Raw | ConvertFrom-Json
    if ($taskPlan.tenantId -ne $taskTenant -or $taskPlan.webAppId -ne $taskClientApp) {throw 'Plan target mismatch.'}
    foreach($taskField in $taskPayload.Keys) {
        if ($taskPlan.request.$taskField -cne $taskPayload[$taskField]) {throw 'Approved payload mismatch.'}
    }
    $taskJournal=[ordered]@{status='STARTED';tenantId=$taskTenant;approvalReference='Human separately approved A-only openid/profile consent packet';request=$taskPayload;grantId=$null;startedUtc=[DateTime]::UtcNow.ToString('o')}
    Save-ReaderJournal
    # Never retry this POST automatically: on an ambiguous response inspect the exact grant tuple first.
    $taskCreated=Invoke-ReaderGraph POST 'oauth2PermissionGrants' $taskPayload
    $taskJournal.grantId=$taskCreated.id;Save-ReaderJournal
    $taskObserved=Invoke-ReaderGraph GET ('oauth2PermissionGrants/'+$taskCreated.id)
    foreach($taskField in @('clientId','consentType','principalId','resourceId')) {
        if ($taskObserved.$taskField -ne $taskPayload[$taskField]) {throw 'Consent readback mismatch.'}
    }
    $taskObservedScopes=@($taskObserved.scope -split '\s+' | Where-Object {$_})
    if ($taskObservedScopes.Count -ne 2 -or (Compare-Object @('openid','profile') $taskObservedScopes)) {throw 'Unexpected consent scope.'}
    $taskJournal.status='VERIFIED';$taskJournal['completedUtc']=[DateTime]::UtcNow.ToString('o');Save-ReaderJournal
    Write-Output 'Verified A-only openid/profile consent; interactive Reader login remains to be checked.'
} catch {
    if ($null -ne $taskJournal) {$taskJournal.status='STOPPED_REVIEW_REQUIRED';$taskJournal['failureCategory']=$_.Exception.GetType().Name;Save-ReaderJournal}
    throw ('Reader consent stopped; no automatic retry or broader grant. Category: '+$_.Exception.GetType().Name)
} finally {$taskHeaders.Clear();if ($null -ne $taskAuth) {$taskAuth.accessToken=$null};$taskAuth=$null}
