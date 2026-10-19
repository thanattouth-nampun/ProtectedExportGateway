param([Parameter(Mandatory = $true)][guid]$TenantId)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($TenantId -eq [guid]::Empty) { throw 'A specific target tenant is required.' }
$repoRoot = Split-Path -Parent $PSScriptRoot
$taskRoot = Join-Path $repoRoot '.runtime/owner-only-inventory'
New-Item -ItemType Directory -Force -Path $taskRoot | Out-Null
# Capture token in process memory only; never print it or pass it on another process command line.
$taskAuth = & az account get-access-token --tenant $TenantId.ToString('D') --resource-type ms-graph --output json --only-show-errors | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or !$taskAuth -or $taskAuth.tenant -ne $TenantId.ToString('D')) { throw 'Target-tenant authentication unavailable; no directory query performed.' }
$taskHeaders = @{ Authorization = 'Bearer ' + $taskAuth.accessToken }
try {
    $taskFilter = "startswith(displayName,'Protected') or startswith(displayName,'Gateway') or startswith(displayName,'MIP') or startswith(displayName,'AIP') or startswith(displayName,'Information Protection')"
    $taskServiceFilter = "startswith(displayName,'Azure Rights') or startswith(displayName,'Microsoft Rights') or startswith(displayName,'Microsoft Information Protection') or startswith(displayName,'Microsoft Purview Information Protection')"
    $taskRequests = @(
        @{ id='domains'; method='GET'; url='/domains?$select=id,isDefault,isInitial,isVerified,authenticationType' },
        @{ id='apps'; method='GET'; url=('/applications?$select=id,appId,displayName,signInAudience,keyCredentials,requiredResourceAccess&$top=100&$filter=' + [Uri]::EscapeDataString($taskFilter)) },
        @{ id='resources'; method='GET'; url=('/servicePrincipals?$select=id,appId,displayName,appRoles&$top=100&$filter=' + [Uri]::EscapeDataString($taskServiceFilter)) }
    )
    # Graph batch uses POST as transport; every enclosed operation is GET.
    $taskBatch = Invoke-RestMethod -Method Post -Uri 'https://graph.microsoft.com/v1.0/$batch' -Headers $taskHeaders -ContentType 'application/json' -Body (@{requests=$taskRequests} | ConvertTo-Json -Depth 8)
    $taskDomains = @($taskBatch.responses | Where-Object id -eq 'domains')[0]
    $taskApps = @($taskBatch.responses | Where-Object id -eq 'apps')[0]
    $taskResources = @($taskBatch.responses | Where-Object id -eq 'resources')[0]
    $taskDomainRows = @(); $taskAppRows = @(); $taskAppTruncated = $false
    $taskResourceRows = @()
    if ($taskDomains.status -eq 200) { $taskDomainRows = @($taskDomains.body.value) }
    if ($taskApps.status -eq 200) {
        $taskAppRows = @($taskApps.body.value | ForEach-Object {
            [ordered]@{ id=$_.id;appId=$_.appId;displayName=$_.displayName;signInAudience=$_.signInAudience
                certificates=@($_.keyCredentials | ForEach-Object { [ordered]@{keyId=$_.keyId;type=$_.type;usage=$_.usage;start=$_.startDateTime;end=$_.endDateTime} })
                requiredResourceAccess=@($_.requiredResourceAccess) }
        })
        $taskAppTruncated = $null -ne $taskApps.body.PSObject.Properties['@odata.nextLink']
    }
    if ($taskResources.status -eq 200) {
        $taskResourceRows = @($taskResources.body.value | ForEach-Object {
            [ordered]@{id=$_.id;appId=$_.appId;displayName=$_.displayName
                relevantApplicationRoles=@($_.appRoles | Where-Object { $_.isEnabled -and $_.allowedMemberTypes -contains 'Application' -and $_.value -in @('Content.DelegatedReader','Content.DelegatedWriter','Content.SuperUser','Content.Writer','UnifiedPolicy.Tenant.Read') } | Select-Object id,value) }
        })
    }
    $taskReport = [ordered]@{
        tenantId=$TenantId.ToString('D');observedAtUtc=[DateTimeOffset]::UtcNow.ToString('O');operations='Graph GET domains, name-filtered application metadata and MIP resource roles'
        domainHttpStatus=$taskDomains.status;appHttpStatus=$taskApps.status;domains=$taskDomainRows
        applicationCandidates=$taskAppRows;candidateSearch='displayName prefixes only; not a complete app inventory';appPageTruncated=$taskAppTruncated
        resourceHttpStatus=$taskResources.status;resourceMetadata=$taskResourceRows
        appSelection='NOT_SELECTED';tenantMutation=$false;adminConsent=$false;liveMipRun=$false
    }
    $taskReport | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $taskRoot 'directory-metadata.json')
    [ordered]@{tenantMatched=$true;domainHttpStatus=$taskDomains.status;verifiedDomains=@($taskDomainRows | Where-Object isVerified | Select-Object id,isDefault,isInitial)
        appHttpStatus=$taskApps.status;candidateCount=$taskAppRows.Count;candidateNames=@($taskAppRows | ForEach-Object {$_.displayName})
        appPageTruncated=$taskAppTruncated;resourceHttpStatus=$taskResources.status;mipResources=$taskResourceRows
        mutations=$false;privateReport='.runtime/owner-only-inventory/directory-metadata.json'} | ConvertTo-Json -Depth 6
} finally {
    $taskHeaders.Clear(); $taskAuth.accessToken = $null; $taskAuth = $null
}
