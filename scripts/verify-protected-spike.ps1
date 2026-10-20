$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repoRoot
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.runtime/dotnet'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.runtime/nuget'
$taskRoot = Join-Path $repoRoot ('.runtime/owner-only-preflight/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $taskRoot | Out-Null
$taskRun = Join-Path $taskRoot 'private-runs'
New-Item -ItemType Directory -Path $taskRun | Out-Null
$taskInput = Join-Path $taskRoot 'not-a-protected-workbook.xlsx'
# Bytes deliberately aren't a workbook: offline preflight must never claim content/security verification.
[IO.File]::WriteAllText($taskInput, 'Synthetic byte-binding preflight fixture only')
$taskHash = (Get-FileHash -LiteralPath $taskInput).Hash
$taskConfig = [ordered]@{
    Processor = [ordered]@{
        TenantId = [guid]::NewGuid().ToString(); ClientId = [guid]::NewGuid().ToString()
        TenantDomain = 'example.test'; CertificateThumbprint = 'NOT-A-CERTIFICATE'
        InputPath = $taskInput; PrivateRunRoot = $taskRun; LabelOnlyId = ''; ConfidentialId = ''
        UdpId = [guid]::NewGuid().ToString(); RecipientA = 'a@example.test'; RecipientB = 'b@example.test'
        ExpectedProtectionOwner = 'owner@example.test'; AllowedResources = @('https://example.test')
        ConsentedHosts = @('example.test'); OperatorAcknowledgedSdkNetworkAndDiagnostics = $true
    }
    ExpectedInputSha256 = $taskHash; ExpectedOriginalOwner = 'owner@example.test'
    AuthorizationReference = 'OFFLINE-TEST-ONLY-NO-LIVE-AUTHORIZATION'
    SyntheticFixtureAcknowledged = $true; OwnerDelegationApproved = $true
}
dotnet build tools/MipFeasibility --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Spike build failed.' }
$taskDll = Join-Path $repoRoot 'tools/MipFeasibility/bin/Debug/net10.0/MipFeasibility.dll'
$taskConfigPath = Join-Path $taskRoot 'owner-only.local.json'
function Invoke-Preflight([int]$expected, [string]$scenario) {
    $taskConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $taskConfigPath
    $taskResult = & dotnet $taskDll owner-only-preflight $taskConfigPath
    if ($LASTEXITCODE -ne $expected) { throw "Unexpected preflight exit: $scenario" }
    $taskObserved = $taskResult | ConvertFrom-Json
    if ($taskObserved.gatePassed -ne $false) { throw 'Preflight must never pass a feasibility gate.' }
    if ($expected -eq 0 -and ($taskObserved.liveAuthentication -ne 'NOT_RUN' -or $taskObserved.protectionInspection -ne 'NOT_RUN')) {
        throw 'Offline preflight must not claim live inspection.'
    }
    Write-Host "PASS: $scenario"
}
Invoke-Preflight 0 'pinned bytes/config only; no certificate lookup, SDK context or network'
$taskConfig.ExpectedInputSha256 = '0' * 64
Invoke-Preflight 2 'hash mismatch rejected'
$taskConfig.ExpectedInputSha256 = $taskHash
$taskConfig.Processor.RecipientB = $taskConfig.Processor.RecipientA
Invoke-Preflight 2 'overlapping recipients rejected'
$taskConfig.Processor.RecipientB = 'b@example.test'
$taskConfig.OwnerDelegationApproved = $false
Invoke-Preflight 2 'missing owner-delegation approval rejected'
$taskConfig.OwnerDelegationApproved = $true
$taskConfig['UnexpectedField'] = 'reject'
Invoke-Preflight 2 'unknown configuration field rejected'
$taskConfig.Remove('UnexpectedField')
$taskConfig['SingleRecipientRestrictedEditorProbe'] = $true
$taskConfig['ExpectedSourceLabelName'] = 'Synthetic test label'
$taskConfig.Processor.RecipientB = ''
$taskConfig.Processor.UdpId = ''
Invoke-Preflight 0 'single-recipient probe permits SDK-only source label discovery; no live inspection'
$taskConfig.Processor.RecipientB = 'b@example.test'
Invoke-Preflight 2 'single-recipient probe rejects a second recipient'
$taskConfig.Processor.RecipientB = ''
$taskConfig.ExpectedSourceLabelName = ''
Invoke-Preflight 2 'single-recipient probe requires expected source label name'
$taskConfig.ExpectedSourceLabelName = 'Synthetic test label'
$taskConfig['MatchExcelReferenceProfile'] = $true
Invoke-Preflight 0 'Excel-reference probe config remains an offline-only preflight'
$taskConfig.SingleRecipientRestrictedEditorProbe = $false
$taskConfig.Processor.RecipientB = 'b@example.test'
$taskConfig.Processor.UdpId = [guid]::NewGuid().ToString()
Invoke-Preflight 2 'Excel-reference profile cannot alter the legacy two-recipient mode'
Write-Host '10 offline preflight checks passed. No protected-source/live gate tested.'
$global:LASTEXITCODE = 0
