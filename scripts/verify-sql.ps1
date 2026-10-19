$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repoRoot
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.runtime/dotnet'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.runtime/nuget'
$env:GATEWAY_SQL_TEST_ROOT = Join-Path $repoRoot '.runtime/sql-tests'
New-Item -ItemType Directory -Force -Path $env:GATEWAY_SQL_TEST_ROOT | Out-Null
if (!(Get-Command SqlLocalDB -ErrorAction SilentlyContinue)) { throw 'SQL LocalDB required; this SQL suite must not be replaced with SQLite or EF InMemory.' }
dotnet test tests/Gateway.SqlTests --logger 'trx;LogFileName=sql-tests.trx'
if ($LASTEXITCODE -ne 0) { throw 'SQL verification failed.' }
