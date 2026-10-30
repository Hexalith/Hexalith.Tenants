param([string]$Launcher = (Join-Path $PSScriptRoot 'run-native-cell.ps1'))
$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('tenants-launcher-guard-' + [Guid]::NewGuid().ToString('N'))
$fixture = Join-Path $root 'fixture'
$browser = Join-Path $fixture 'tests\Hexalith.Tenants.UI.Tests\Browser'
$null = New-Item -ItemType Directory -Path $browser -Force
$fake = @'
param($FixtureRoot,$OutputDirectory,$BrowserEngine,$TrialGroup,$Cultures,$Trials)
[IO.File]::WriteAllText((Join-Path $FixtureRoot 'collector-was-started'), 'unexpected')
exit 9
'@
[IO.File]::WriteAllText((Join-Path $browser 'capture-deferred-nvda-speech.ps1'), $fake)
$cases = @('directory','sidecar','both')
try {
    foreach ($case in $cases) {
        $output = Join-Path $root $case
        $sidecar = $output + '-invocation.json'
        if ($case -ne 'sidecar') { $null = New-Item -ItemType Directory -Path $output }
        if ($case -ne 'directory') { [IO.File]::WriteAllText($sidecar, "original`r`ninvocation") }
        $original = if ([IO.File]::Exists($sidecar)) { [Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecar)) } else { $null }
        $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$Launcher+'"'),
            '-FixtureRoot',('"'+$fixture+'"'),'-OutputDirectory',('"'+$output+'"'),'-BrowserEngine','chromium')
        $child = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden -RedirectStandardOutput (Join-Path $root ($case+'.out')) -RedirectStandardError (Join-Path $root ($case+'.err'))
        if ($child.ExitCode -eq 0) { throw 'Existing output was accepted.' }
        if ($original) {
            if ([Convert]::ToBase64String([IO.File]::ReadAllBytes($sidecar)) -ne $original) { throw 'Original invocation changed.' }
        } elseif ([IO.File]::Exists($sidecar)) { throw 'Directory refusal created a sidecar.' }
        if ([IO.File]::Exists((Join-Path $fixture 'collector-was-started'))) { throw 'Collector launched before refusal.' }
    }
    [ordered]@{ existingOutputCases=3; originalBytesPreserved=$true; collectorStarted=$false; passed=$true } | ConvertTo-Json
} finally { Remove-Item -LiteralPath $root -Recurse -Force }
