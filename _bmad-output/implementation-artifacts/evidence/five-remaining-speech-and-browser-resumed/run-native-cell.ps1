param([Parameter(Mandatory=$true)][string]$FixtureRoot,
      [Parameter(Mandatory=$true)][string]$OutputDirectory,
      [Parameter(Mandatory=$true)][ValidateSet('chromium','firefox')][string]$BrowserEngine,
      [string]$Trial = 'S0-en',
      [switch]$FullMatrix)
$ErrorActionPreference = 'Stop'
$sidecarPath = $OutputDirectory + '-invocation.json'
if ((Test-Path -LiteralPath $OutputDirectory) -or (Test-Path -LiteralPath $sidecarPath)) {
    throw 'Use a fresh output directory and invocation sidecar; earlier evidence is never overwritten.'
}
$encoding = New-Object Text.UTF8Encoding($false)
$state = [ordered]@{ startedUtc=[DateTime]::UtcNow.ToString('o'); fixtureRoot=$FixtureRoot;
    outputDirectory=$OutputDirectory; browserEngine=$BrowserEngine; trial=$Trial; fullMatrix=[bool]$FullMatrix }
Set-Location -LiteralPath $FixtureRoot
$collector = Join-Path $FixtureRoot 'tests\Hexalith.Tenants.UI.Tests\Browser\capture-deferred-nvda-speech.ps1'
$arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$collector,
    '-FixtureRoot',$FixtureRoot,'-OutputDirectory',$OutputDirectory,'-BrowserEngine',$BrowserEngine)
if (-not $FullMatrix) { $arguments += @('-TrialGroup','search','-Cultures','en','-Trials',$Trial) }
$state.executable = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$state.arguments = $arguments
$state.nvdaProcessesBefore = @(Get-Process -Name nvda,nvda_noUIAccess,nvda_uiAccess -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
$sidecar = [IO.File]::Open($sidecarPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
function Save-OwnedInvocation {
    $bytes = $encoding.GetBytes(($state | ConvertTo-Json -Depth 12))
    $sidecar.Position = 0; $sidecar.SetLength(0)
    $sidecar.Write($bytes, 0, $bytes.Length); $sidecar.Flush()
}
try {
    Save-OwnedInvocation
    & $state.executable @arguments
    $state.exitCode = $LASTEXITCODE
    $state.endedUtc = [DateTime]::UtcNow.ToString('o')
    $state.nvdaProcessesAfter = @(Get-Process -Name nvda,nvda_noUIAccess,nvda_uiAccess -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
    Save-OwnedInvocation
} finally { $sidecar.Dispose() }
exit $state.exitCode
