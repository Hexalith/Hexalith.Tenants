param([Parameter(Mandatory=$true)][string]$FixtureRoot,
      [Parameter(Mandatory=$true)][string]$OutputDirectory,
      [Parameter(Mandatory=$true)][ValidateSet('chromium','firefox')][string]$BrowserEngine,
      [string]$Trial = 'S0-en')
$ErrorActionPreference = 'Stop'
$encoding = New-Object Text.UTF8Encoding($false)
$state = [ordered]@{ startedUtc=[DateTime]::UtcNow.ToString('o'); fixtureRoot=$FixtureRoot;
    outputDirectory=$OutputDirectory; browserEngine=$BrowserEngine; trial=$Trial }
Set-Location -LiteralPath $FixtureRoot
$collector = Join-Path $FixtureRoot 'tests\Hexalith.Tenants.UI.Tests\Browser\capture-deferred-nvda-speech.ps1'
$arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$collector,
    '-FixtureRoot',$FixtureRoot,'-OutputDirectory',$OutputDirectory,'-BrowserEngine',$BrowserEngine,
    '-TrialGroup','search','-Cultures','en','-Trials',$Trial)
$state.executable = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$state.arguments = $arguments
$state.nvdaProcessesBefore = @(Get-Process -Name nvda,nvda_noUIAccess,nvda_uiAccess -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
[IO.File]::WriteAllText(($OutputDirectory + '-invocation.json'), ($state | ConvertTo-Json -Depth 12), $encoding)
& $state.executable @arguments
$state.exitCode = $LASTEXITCODE
$state.endedUtc = [DateTime]::UtcNow.ToString('o')
$state.nvdaProcessesAfter = @(Get-Process -Name nvda,nvda_noUIAccess,nvda_uiAccess -ErrorAction SilentlyContinue | Select-Object Id,ProcessName)
[IO.File]::WriteAllText(($OutputDirectory + '-invocation.json'), ($state | ConvertTo-Json -Depth 12), $encoding)
exit $state.exitCode
