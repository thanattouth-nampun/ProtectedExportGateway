$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repoRoot
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.runtime/dotnet'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.runtime/nuget'
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $repoRoot '.runtime/playwright'
function Invoke-Checked([scriptblock]$Action) {
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "Verification command failed: exit $LASTEXITCODE" }
}
Invoke-Checked { dotnet restore ProtectedExportGateway.slnx --locked-mode }
Push-Location src/Gateway.Client
try {
    Invoke-Checked { npm.cmd ci --ignore-scripts }
    Invoke-Checked { npm.cmd run build }
    Invoke-Checked { npm.cmd test }
    Invoke-Checked { .\node_modules\.bin\playwright.cmd install chromium }
    Invoke-Checked { npm.cmd run test:browser }
} finally { Pop-Location }
Invoke-Checked { dotnet test tests/Gateway.UnitTests --no-restore }
& (Join-Path $PSScriptRoot 'verify-sql.ps1')
$env:GATEWAY_EXPORT_OPENAPI = Join-Path $repoRoot 'src/Gateway.Web/openapi.json'
$beforeContract = (Get-FileHash src/Gateway.Client/src/contracts/api.d.ts).Hash
Invoke-Checked { dotnet test tests/Gateway.IntegrationTests --no-restore }
Push-Location src/Gateway.Client
try { Invoke-Checked { npm.cmd run contracts } } finally { Pop-Location }
if ((Get-FileHash src/Gateway.Client/src/contracts/api.d.ts).Hash -ne $beforeContract) {
    throw 'Generated API contract drifted. Review and regenerate the frontend contract before release.'
}
Invoke-Checked { dotnet run --project tools/MipFeasibility --no-restore -- native-check }
$publishPath = [IO.Path]::GetFullPath((Join-Path $repoRoot '.runtime/publish'))
if (!$publishPath.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Publish path must remain inside this workspace.'
}
if (Test-Path -LiteralPath $publishPath) { Remove-Item -LiteralPath $publishPath -Recurse -Force }
Invoke-Checked { dotnet publish src/Gateway.Web -c Release -o .runtime/publish }
if (!(Test-Path .runtime/publish/wwwroot/app/index.html)) { throw 'Single web artifact is missing React assets.' }
if (Test-Path .runtime/publish/wwwroot/app/spike.html) { throw 'Development harness must not ship.' }
if (Get-ChildItem .runtime/publish/wwwroot/app/assets -Filter '*VaultScene*') { throw 'Retired renderer must not ship.' }
$clientLock = Get-Content src/Gateway.Client/package-lock.json -Raw
if ($clientLock -match 'node_modules/(three|three-stdlib|@react-three/[^"/]+)"') { throw 'Retired renderer dependencies remain in the lockfile.' }
Write-Host 'Local checkpoint verified. Live G0-G7 acceptance remains separate.'
