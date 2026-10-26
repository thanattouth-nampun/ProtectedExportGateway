$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$taskRepo=Split-Path -Parent $PSScriptRoot
$taskRoot=[IO.Path]::GetFullPath((Join-Path $taskRepo '.runtime/quarantine-spike'))
if(!$taskRoot.StartsWith($taskRepo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Root outside workspace'}
$taskObjects=Join-Path $taskRoot 'objects'
$taskConfigPath=Join-Path $taskRoot 'web.local.json'
if((Test-Path -LiteralPath $taskObjects) -or (Test-Path -LiteralPath $taskConfigPath)) {throw 'Existing quarantine configuration/root: inspect it; never reset permissions or overwrite data.'}
for($taskParent=[IO.DirectoryInfo]::new($taskRoot);$null -ne $taskParent;$taskParent=$taskParent.Parent) {
    if($taskParent.Exists -and ($taskParent.Attributes -band [IO.FileAttributes]::ReparsePoint)) {throw 'Reparse ancestor refused'}
}
$taskConfig=Get-Content (Join-Path $taskRepo '.runtime/owner-web/web.local.json') -Raw | ConvertFrom-Json
if($taskConfig.WebIdentity.TenantId -ne '9fbc4f5d-56ec-4074-82f0-69d9a86a7c06' -or $taskConfig.WebIdentity.ClientId -ne 'a834c776-387c-43aa-8d55-3ab62a08acf6') {throw 'Unexpected existing local identity'}
New-Item -ItemType Directory -Path $taskObjects -Force | Out-Null
$taskAcl=[Security.AccessControl.DirectorySecurity]::new();$taskAcl.SetAccessRuleProtection($true,$false)
$taskCurrent=[Security.Principal.WindowsIdentity]::GetCurrent().User
foreach($taskSid in @($taskCurrent,[Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
    $taskAcl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($taskSid,[Security.AccessControl.FileSystemRights]::FullControl,
        [Security.AccessControl.InheritanceFlags]'ContainerInherit,ObjectInherit',[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow))
}
Set-Acl -LiteralPath $taskObjects -AclObject $taskAcl
$taskConfig | Add-Member -MemberType NoteProperty -Name Quarantine -Value ([ordered]@{EnableLocalSpike=$true;SyntheticOnlyAcknowledged=$true;PrivateRoot=$taskObjects})
$taskConfig | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskConfigPath
Write-Output 'Prepared Development-only local quarantine configuration/private root. No tenant call, DB migration, scan, source promotion or publish performed.'
