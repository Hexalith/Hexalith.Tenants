param([Parameter(Mandatory=$true)][string]$FixtureRoot,
      [Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$encoding = New-Object Text.UTF8Encoding($false)
$result = [ordered]@{ startedUtc = [DateTime]::UtcNow.ToString('o'); fixtureRoot = $FixtureRoot; commands = @() }
function Get-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $Path).Path)
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
function Save-Result { [IO.File]::WriteAllText($OutputPath, ($result | ConvertTo-Json -Depth 12), $encoding) }
function Invoke-Recorded([string]$Executable, [string[]]$Arguments) {
    $start = [DateTime]::UtcNow.ToString('o')
    $lines = & $Executable @Arguments 2>&1
    $code = $LASTEXITCODE
    $result.commands += [ordered]@{ executable=$Executable; arguments=$Arguments; cwd=(Get-Location).Path;
        startedUtc=$start; endedUtc=[DateTime]::UtcNow.ToString('o'); exitCode=$code; output=($lines | Out-String) }
    Save-Result
    if ($code -ne 0) { throw "Prerequisite command failed: $Executable" }
}
Set-Location -LiteralPath $FixtureRoot
$result.os = [Environment]::OSVersion.Version.ToString()
$result.nvdaProcessesBefore = @(Get-Process -Name nvda,nvda_noUIAccess,nvda_uiAccess -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
$result.nvdaExecutableVersion = (Get-Item 'C:\Program Files\NVDA\nvda_noUIAccess.exe').VersionInfo.FileVersion
$settingsPath = Join-Path $env:APPDATA 'nvda\nvda.ini'
$result.originalSettingsHash = if (Test-Path -LiteralPath $settingsPath) { Get-Sha256 $settingsPath } else { $null }
Save-Result
if ($result.nvdaProcessesBefore.Count -gt 0) { throw 'Existing NVDA session preserved; capture prerequisite blocked.' }
Invoke-Recorded 'dotnet' @('--version')
Invoke-Recorded 'node' @('--version')
Invoke-Recorded 'npm.cmd' @('--version')
Invoke-Recorded 'git' @('--version')
Set-Location -LiteralPath (Join-Path $FixtureRoot 'tests\performance\tenant-audit')
$result.npmLockHashBefore = Get-Sha256 'package-lock.json'
Invoke-Recorded 'npm.cmd' @('ci')
$result.npmLockHashAfter = Get-Sha256 'package-lock.json'
Invoke-Recorded 'npx.cmd' @('playwright','install','firefox')
$result.endedUtc = [DateTime]::UtcNow.ToString('o')
$result.completed = $true
Save-Result
