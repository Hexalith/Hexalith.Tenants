<#
.SYNOPSIS
Opt-in native Windows/NVDA Speech Viewer collection for the disposable fixture.
.DESCRIPTION
Run in a disposable native checkout. Uses private NVDA config and owned child
processes only. Refuses an existing NVDA session. Queued Speech Viewer output
does not establish audible completion. Raw logs remain in the temporary run dir;
review and sanitize speech before copying evidence into the repository.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [ValidateSet('chromium', 'firefox')][string]$BrowserEngine = 'chromium',
    [ValidateSet('all', 'search', 'create')][string]$TrialGroup = 'all',
    [ValidateSet('en', 'fr')][string[]]$Cultures = @('en', 'fr'),
    [ValidatePattern('^(S[0-3]|C[12]-(lifecycle|availability)|C3-(events|unavailable)-(lifecycle|availability))-(en|fr)$')][string[]]$Trials = @(),
    [string]$NvdaPath = 'C:\Program Files\NVDA\nvda_noUIAccess.exe',
    [string]$SettingsPath = "$env:APPDATA\nvda\nvda.ini",
    [string]$FixtureRoot = (Resolve-Path "$PSScriptRoot\..\..\..").Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Native Windows is required.' }
if (Get-Process -Name nvda, nvda_noUIAccess, nvda_uiAccess -ErrorAction SilentlyContinue) {
    throw 'An NVDA session is already running. Preserve it; run this capture in a separate disposable desktop session.'
}
if (-not (Test-Path -LiteralPath $NvdaPath -PathType Leaf)) { throw 'NVDA executable is missing.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a new output directory; earlier evidence is never overwritten.' }
$null = New-Item -ItemType Directory -Path $OutputDirectory
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$privateRoot = Join-Path ([IO.Path]::GetTempPath()) ('tenants-native-speech-' + [Guid]::NewGuid().ToString('N'))
$configRoot = Join-Path $privateRoot 'nvda'
$null = New-Item -ItemType Directory -Path (Join-Path $configRoot 'scratchpad\globalPlugins') -Force
$encoding = New-Object Text.UTF8Encoding($false)
function Get-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
$settings = if (Test-Path -LiteralPath $SettingsPath) { [IO.File]::ReadAllText($SettingsPath) } else { '' }
$originalSettingsHash = if (Test-Path -LiteralPath $SettingsPath) { Get-Sha256 $SettingsPath } else { $null }
function Set-IniValue([string]$Text, [string]$Section, [string]$Key, [string]$Value) {
    $pattern = '(?ms)^\[' + [regex]::Escape($Section) + '\]\s*\r?\n(?<body>.*?)(?=^\[|\z)'
    $match = [regex]::Match($Text, $pattern)
    if (-not $match.Success) { return $Text + "`r`n[$Section]`r`n$Key = $Value`r`n" }
    $body = $match.Groups['body'].Value
    $line = '(?m)^\s*' + [regex]::Escape($Key) + '\s*=.*$'
    if ([regex]::IsMatch($body, $line)) { $body = [regex]::Replace($body, $line, "$Key = $Value") }
    else { $body += "$Key = $Value`r`n" }
    return $Text.Substring(0, $match.Index) + "[$Section]`r`n" + $body + $Text.Substring($match.Index + $match.Length)
}
$settings = Set-IniValue $settings 'general' 'showWelcomeDialogAtStartup' 'False'
$settings = Set-IniValue $settings 'general' 'language' 'en'
$settings = Set-IniValue $settings 'speechViewer' 'showSpeechViewerAtStartup' 'True'
$settings = Set-IniValue $settings 'development' 'enableScratchpadDir' 'True'
$settings = Set-IniValue $settings 'update' 'autoCheck' 'False'
$settings = Set-IniValue $settings 'update' 'startupNotification' 'False'
$settings = Set-IniValue $settings 'remote' 'enabled' 'False'
[IO.File]::WriteAllText((Join-Path $configRoot 'nvda.ini'), $settings, $encoding)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'deferred-nvda-observer.py') -Destination (Join-Path $configRoot 'scratchpad\globalPlugins\tenantsSpeechObserver.py')
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class TenantsSpeechDesktop {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, UIntPtr wparam, System.Text.StringBuilder text, uint flags, uint timeout, out UIntPtr result);
    public static string ReadText(IntPtr hwnd) {
        UIntPtr length;
        if (SendMessageTimeout(hwnd, 0x000E, UIntPtr.Zero, null, 2, 1000, out length) == IntPtr.Zero) throw new InvalidOperationException("Owned viewer length read timed out");
        int count = checked((int)length.ToUInt64());
        if (count > 1000000) throw new InvalidOperationException("Owned viewer exceeded the capture bound");
        var text = new System.Text.StringBuilder(count + 1);
        UIntPtr copied;
        if (SendMessageTimeout(hwnd, 0x000D, (UIntPtr)(count + 1), text, 2, 1000, out copied) == IntPtr.Zero) throw new InvalidOperationException("Owned viewer text read timed out");
        return text.ToString();
    }
}
'@
$entryForeground = [TenantsSpeechDesktop]::GetForegroundWindow()
$started = [DateTime]::UtcNow
$ownedNvda = @()
$node = $null
$nodeStarted = $false
$stderr = $null
$plannedTrials = @()
$observedTrials = New-Object 'Collections.Generic.HashSet[string]'
$verifiedControls = New-Object 'Collections.Generic.HashSet[string]'
$armedTrials = New-Object 'Collections.Generic.HashSet[string]'
$activeTrial = $null
$lastCancellationUtc = $null
$initialProgressMilliseconds = 180000
$protocolProgressMilliseconds = 90000
$viewer = $null
$viewerText = ''
$lastSpeech = [DateTime]::UtcNow
$trial = $null
$capture = $false
$browserHandle = [IntPtr]::Zero
$browserProcess = 0
$interruptionOffset = 0
$transcript = New-Object Collections.Generic.List[object]
$protocol = New-Object Collections.Generic.List[object]
$metadata = [ordered]@{
    startedUtc = $started.ToString('o'); engine = $BrowserEngine; trialGroup = $TrialGroup; cultures = $Cultures; requestedTrials = $Trials
    osVersion = [Environment]::OSVersion.VersionString
    nvdaVersion = (Get-Item -LiteralPath $NvdaPath).VersionInfo.ProductVersion
    viewerSource = 'NVDA-owned Speech Viewer edit control, read without changing focus'
    timing = 'UTC sample time at 100 ms polling; queued text arrival, not word onset or audible completion'
    settingsOrigin = $(if ($originalSettingsHash) { 'private copy of existing nvda.ini; no addons copied' } else { 'private NVDA default configuration' })
    settingsOverrides = @('English NVDA interface', 'welcome dialog disabled', 'Speech Viewer shown on startup', 'private read-only scratchpad settings observer enabled', 'private automatic update checks/notifications disabled', 'private remote sessions disabled')
    sourceSettingsSha256 = $originalSettingsHash
    collectorSha256 = Get-Sha256 $PSCommandPath
    observerSha256 = Get-Sha256 (Join-Path $PSScriptRoot 'deferred-nvda-observer.py')
    outputSanitized = $false; passed = $false; completed = $false
}
function Get-ViewerText {
    return [TenantsSpeechDesktop]::ReadText([IntPtr]$viewer.Current.NativeWindowHandle)
}
function Reset-CaptureBaseline {
    $script:viewerText = Get-ViewerText
    $script:lastSpeech = [DateTime]::UtcNow
    $path = Join-Path $configRoot 'interruptions.jsonl'
    $script:interruptionOffset = if (Test-Path -LiteralPath $path) { @([IO.File]::ReadAllLines($path)).Count } else { 0 }
}
function Record-ForegroundLoss([string]$Stage) {
    [uint32]$otherProcess = 0
    $handle = [TenantsSpeechDesktop]::GetForegroundWindow()
    $null = [TenantsSpeechDesktop]::GetWindowThreadProcessId($handle, [ref]$otherProcess)
    $owner = if (@($ownedNvda | ForEach-Object { $_.Id }) -contains $otherProcess) { 'owned-nvda' } elseif ($otherProcess -eq $browserProcess) { 'owned-browser-other-window' } else { 'outside-owned-browser-and-nvda' }
    $process = Get-Process -Id $otherProcess -ErrorAction SilentlyContinue
    $script:protocol.Add(@{ kind = 'foreground-lost'; trial = $script:trial; utc = [DateTime]::UtcNow.ToString('o'); stage = $Stage
        owner = $owner; processBasename = $(if ($process) { $process.ProcessName } else { 'unavailable' })
        expectedWindow = $script:browserHandle.ToInt64(); actualWindow = $handle.ToInt64() })
}
function Get-UtcDate([object]$Value) {
    if ($Value -is [DateTime]) { return $Value.ToUniversalTime() }
    if ($Value -is [DateTimeOffset]) { return $Value.UtcDateTime }
    return [DateTime]::Parse([string]$Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
}
function Get-FreshMode {
    $path = Join-Path $configRoot 'effective-mode.json'
    if (-not (Test-Path -LiteralPath $path)) { throw 'NVDA mode observation is missing.' }
    $mode = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $age = ([DateTime]::UtcNow - (Get-UtcDate $mode.observedUtc)).TotalMilliseconds
    if ($age -lt 0 -or $age -gt 2000 -or $mode.mode -notin @('browse', 'focus', 'not-applicable') -or $mode.speechMode -ne 'talk') {
        throw 'NVDA mode observation is stale or invalid.'
    }
    return $mode
}
function Get-ExpectedTrials {
    $items = New-Object 'Collections.Generic.HashSet[string]'
    if ($Cultures.Count -eq 0 -or @($Cultures | Select-Object -Unique).Count -ne $Cultures.Count) { throw 'Invalid or duplicate cultures.' }
    if ($Trials.Count) {
        foreach ($item in $Trials) {
            $culture = $item.Substring($item.Length - 2)
            if ($culture -notin $Cultures -or ($TrialGroup -eq 'search' -and -not $item.StartsWith('S')) -or ($TrialGroup -eq 'create' -and $item.StartsWith('S'))) { throw 'Requested trial is excluded by its culture/group selectors.' }
            if (-not $items.Add($item)) { throw 'Duplicate requested trial.' }
        }
    } else {
        foreach ($culture in $Cultures) {
            if ($TrialGroup -ne 'create') { foreach ($case in @('S0', 'S1', 'S2', 'S3')) { $null = $items.Add("$case-$culture") } }
            if ($TrialGroup -ne 'search') { foreach ($status in @('events', 'unavailable')) { foreach ($refresh in @('lifecycle', 'availability')) {
                $first = if ($status -eq 'events') { 'C1' } else { 'C2' }
                $null = $items.Add("$first-$refresh-$culture"); $null = $items.Add("C3-$status-$refresh-$culture")
            } } }
        }
    }
    foreach ($item in @($items)) {
        if ($item -match '^C3-(events|unavailable)-(lifecycle|availability)-(en|fr)$') {
            $first = if ($Matches[1] -eq 'events') { 'C1' } else { 'C2' }
            $null = $items.Add("$first-$($Matches[2])-$($Matches[3])")
        }
    }
    return @($items | Sort-Object)
}
function Assert-CompletedCoverage([object[]]$Completed) {
    if ($plannedTrials.Count -eq 0 -or $Completed.Count -ne $plannedTrials.Count -or @($Completed | Select-Object -Unique).Count -ne $Completed.Count) { throw 'Native protocol completion has empty or duplicate coverage.' }
    foreach ($items in @(@($Completed), @($observedTrials), @($verifiedControls), @($armedTrials))) {
        if ($items.Count -ne $plannedTrials.Count -or @(Compare-Object -ReferenceObject $plannedTrials -DifferenceObject $items).Count) { throw 'Native observation/control coverage does not match the requested plan.' }
    }
    if ($transcript.Count -eq 0) { throw 'Native protocol completed without captured positive-control speech.' }
}
function Sample-Speech {
    if ($script:capture -and [TenantsSpeechDesktop]::GetForegroundWindow() -ne $script:browserHandle) {
        Record-ForegroundLoss 'speech-sample'
        throw 'Browser foreground was lost; no unrelated desktop output is retained.'
    }
    $mode = if ($script:capture) { Get-FreshMode } else { $null }
    $interruptionPath = Join-Path $configRoot 'interruptions.jsonl'
    if (Test-Path -LiteralPath $interruptionPath) {
        $lines = @([IO.File]::ReadAllLines($interruptionPath))
        if ($script:capture -and $lines.Count -gt $script:interruptionOffset) {
            foreach ($line in $lines[$script:interruptionOffset..($lines.Count - 1)]) {
                $event = $line | ConvertFrom-Json
                $script:protocol.Add(@{ kind = $event.kind; utc = $event.utc; trial = $script:trial })
                if ($event.kind -eq 'nvda-speech-canceled') { $script:lastCancellationUtc = Get-UtcDate $event.utc }
            }
        }
        $script:interruptionOffset = $lines.Count
    }
    $current = Get-ViewerText
    if ($current -eq $script:viewerText) { return }
    if (-not $current.StartsWith($script:viewerText, [StringComparison]::Ordinal)) {
        $script:protocol.Add(@{ kind = 'viewer-text-reset'; trial = $script:trial; oldLength = $script:viewerText.Length; newLength = $current.Length; utc = [DateTime]::UtcNow.ToString('o') })
        throw 'Viewer text was reset or truncated; this capture cannot be accepted.'
    }
    $delta = $current.Substring($script:viewerText.Length)
    $script:viewerText = $current
    $script:lastSpeech = [DateTime]::UtcNow
    if ($script:capture) {
        [uint32]$foregroundProcess = 0
        $foreground = [TenantsSpeechDesktop]::GetForegroundWindow()
        $null = [TenantsSpeechDesktop]::GetWindowThreadProcessId($foreground, [ref]$foregroundProcess)
        if ($foreground -ne $script:browserHandle) { Record-ForegroundLoss 'text-arrival'; throw 'Browser foreground was lost during speech capture; unrelated desktop speech is excluded.' }
        $sample = [ordered]@{ utc = $script:lastSpeech.ToString('o'); trial = $script:trial
            browserForeground = $true; nvdaMode = $mode; text = $delta }
        $script:transcript.Add($sample)
        [IO.File]::AppendAllText((Join-Path $OutputDirectory 'speech.jsonl'), ($sample | ConvertTo-Json -Compress -Depth 6) + "`n", $encoding)
    }
}
function Wait-Quiet([int]$MinimumMilliseconds, [DateTime]$Deadline) {
    while ([DateTime]::UtcNow -lt $Deadline) {
        Sample-Speech
        if ([TenantsSpeechDesktop]::GetForegroundWindow() -ne $script:browserHandle) { Record-ForegroundLoss 'quiet-window'; throw 'Browser foreground was lost during the observation.' }
        if (([DateTime]::UtcNow - $script:lastSpeech).TotalMilliseconds -ge $MinimumMilliseconds) { return }
        Start-Sleep -Milliseconds 100
    }
    throw 'No five-second quiet window within 60 seconds; retain this attempt as inconclusive.'
}
try {
    $expectedTrials = @(Get-ExpectedTrials)
    $launchedNvda = Start-Process -FilePath $NvdaPath -ArgumentList @('--config-path', ('"' + $configRoot + '"'), '--log-file', ('"' + (Join-Path $privateRoot 'nvda.log') + '"')) -PassThru
    $ownedNvda = @($launchedNvda)
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    while (-not $viewer -and [DateTime]::UtcNow -lt $deadline) {
        $ownedNvda = @(Get-Process -Id $launchedNvda.Id -ErrorAction SilentlyContinue)
        $condition = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::NameProperty, 'NVDA Speech Viewer')
        $windows = [Windows.Automation.AutomationElement]::RootElement.FindAll([Windows.Automation.TreeScope]::Children, $condition)
        foreach ($window in $windows) {
            if (@($ownedNvda | ForEach-Object { $_.Id }) -contains $window.Current.ProcessId) {
                $editCondition = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ClassNameProperty, 'RICHEDIT50W')
                $edits = $window.FindAll([Windows.Automation.TreeScope]::Descendants, $editCondition)
                if ($edits.Count -eq 1) {
                    $viewer = $edits[0]
                    $metadata.viewerProcessId = $window.Current.ProcessId
                    $metadata.viewerControlClass = $viewer.Current.ClassName
                    $metadata.viewerControlType = $viewer.Current.ControlType.ProgrammaticName
                    break
                }
            }
        }
        if (-not $viewer) { Start-Sleep -Milliseconds 100 }
    }
    if (-not $viewer) { throw 'No edit control bound to the owned NVDA Speech Viewer was found.' }
    $viewerText = Get-ViewerText
    $metadata.viewerBound = $true
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command node).Source
    $start.WorkingDirectory = $FixtureRoot
    $runner = Join-Path $FixtureRoot 'tests\Hexalith.Tenants.UI.Tests\Browser\validate-authenticated-administrator-dialogs.mjs'
    $packet = Join-Path $OutputDirectory 'fixture.json'
    $start.Arguments = '"' + $runner + '" "' + $packet + '"'
    $start.UseShellExecute = $false
    $start.RedirectStandardInput = $true; $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    $start.CreateNoWindow = $true
    $start.EnvironmentVariables['TENANTS_BROWSER_HANDOFF'] = 'native'
    $start.EnvironmentVariables['TENANTS_BROWSER_ENGINE'] = $BrowserEngine
    $start.EnvironmentVariables['TENANTS_NATIVE_SPEECH_CAPTURE'] = '1'
    $start.EnvironmentVariables['TENANTS_NATIVE_SPEECH_GROUP'] = $TrialGroup
    $start.EnvironmentVariables['TENANTS_NATIVE_SPEECH_CULTURES'] = $Cultures -join ','
    $start.EnvironmentVariables['TENANTS_NATIVE_SPEECH_TRIALS'] = $Trials -join ','
    if ($BrowserEngine -eq 'chromium') { $start.EnvironmentVariables['TENANTS_BROWSER_CHANNEL'] = 'chrome' }
    else { $start.EnvironmentVariables.Remove('TENANTS_BROWSER_CHANNEL') }
    $node = New-Object Diagnostics.Process
    $node.StartInfo = $start
    $nodeStarted = $node.Start()
    if (-not $nodeStarted) { throw 'Owned native runner could not start.' }
    $stderr = $node.StandardError.ReadToEndAsync()
    $lineTask = $node.StandardOutput.ReadLineAsync()
    $progressDeadline = [DateTime]::UtcNow.AddMilliseconds($initialProgressMilliseconds)
    while (-not $node.HasExited -or $null -ne $lineTask) {
        if ([DateTime]::UtcNow -gt $progressDeadline) { throw 'Native runner protocol progress timed out; retain this failure packet.' }
        if ($capture) { Sample-Speech }
        if (-not $lineTask.IsCompleted) { Start-Sleep -Milliseconds 100; continue }
        $line = $lineTask.GetAwaiter().GetResult()
        if ($null -eq $line) { $lineTask = $null; break }
        [IO.File]::AppendAllText((Join-Path $privateRoot 'runner.stdout'), $line + "`r`n", $encoding)
        $lineTask = $node.StandardOutput.ReadLineAsync()
        if (-not $line.StartsWith('{"nativeSpeech":true,')) { continue }
        $event = $line | ConvertFrom-Json
        $protocol.Add($event)
        $response = @{ sequence = $event.sequence; ok = $true }
        switch ($event.kind) {
            'plan' {
                if ($plannedTrials.Count) { throw 'Native plan was repeated.' }
                $plannedTrials = @($event.requiredTrials)
                if ($plannedTrials.Count -eq 0 -or $plannedTrials.Count -ne $expectedTrials.Count -or @(Compare-Object -ReferenceObject $expectedTrials -DifferenceObject $plannedTrials).Count) { throw 'Native plan does not match the selectors.' }
            }
            'session' {
                $capture = $false
                $windowDeadline = [DateTime]::UtcNow.AddSeconds(5)
                do {
                    $candidate = @(Get-Process -Name chrome, firefox -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like 'Tenants native speech fixture*' -and $_.StartTime.ToUniversalTime() -ge $started })
                    if ($candidate.Count -eq 1) { break }
                    Start-Sleep -Milliseconds 100
                } while ([DateTime]::UtcNow -lt $windowDeadline)
                if ($candidate.Count -ne 1) { throw "The disposable browser window is not unique (matching windows: $($candidate.Count))." }
                $browserProcess = $candidate[0].Id; $browserHandle = $candidate[0].MainWindowHandle
                # Never foreground the terminal or viewer; only this uniquely titled disposable browser.
                $null = [TenantsSpeechDesktop]::SetForegroundWindow($browserHandle)
                if ([TenantsSpeechDesktop]::GetForegroundWindow() -ne $browserHandle) { Record-ForegroundLoss 'session-binding'; throw 'The native fixture browser is not in the foreground.' }
            }
            'arm' {
                if ($event.trial -notin $plannedTrials -or -not $armedTrials.Add($event.trial)) { throw 'Unexpected or repeated native trial.' }
                $activeTrial = $event.trial; $trial = $event.trial; Reset-CaptureBaseline; $capture = $true
            }
            'quiet' { Wait-Quiet 5000 ([DateTime]::UtcNow.AddSeconds(60)); $protocol.Add(@{ kind = 'quiet-window'; trial = $trial; utc = [DateTime]::UtcNow.ToString('o'); queuedQuietSeconds = 5 }) }
            'trigger' { Sample-Speech }
            'observe' {
                if ($event.trial -ne $activeTrial -or $observedTrials.Contains($event.trial)) { throw 'Unmatched or duplicate observation.' }
                $observationStart = [DateTime]::UtcNow
                $deadline = (Get-UtcDate $event.stateUtc).AddSeconds(60)
                $minimumEnd = (Get-UtcDate $event.stateUtc).AddSeconds(30)
                while ([DateTime]::UtcNow -lt $minimumEnd) { Sample-Speech; if ([TenantsSpeechDesktop]::GetForegroundWindow() -ne $browserHandle) { Record-ForegroundLoss 'observation'; throw 'Browser foreground was lost.' }; Start-Sleep -Milliseconds 100 }
                Wait-Quiet 5000 $deadline
                $response.observation = @{ startedUtc = $observationStart.ToString('o'); endedUtc = [DateTime]::UtcNow.ToString('o'); queuedQuietSeconds = 5; audibleCompletion = 'not established by Speech Viewer' }
                $protocol.Add(@{ kind = 'observation-end'; trial = $trial; observation = $response.observation })
                $null = $observedTrials.Add($event.trial)
                $capture = $false
            }
            'control-start' {
                if ($event.trial -ne $activeTrial -or -not $observedTrials.Contains($event.trial)) { throw 'Unmatched positive control.' }
                $trial = $event.trial + '-focus-control'; Reset-CaptureBaseline; $capture = $true
            }
            'control-end' {
                if ($event.trial -ne $activeTrial -or $event.culture -ne $activeTrial.Substring($activeTrial.Length - 2)) { throw 'Unmatched control identity/culture.' }
                Start-Sleep -Milliseconds 500
                Wait-Quiet 5000 ([DateTime]::UtcNow.AddSeconds(60))
                $samples = @($transcript | Where-Object { $_.trial -eq $trial })
                $spoken = ($samples | ForEach-Object { $_.text }) -join "`n"
                $expectedName = if ($event.culture -eq 'fr') { 'Rechercher des locataires' } else { 'Search tenants' }
                $focusMatches = $event.focus.documentHasFocus -and ('tenants-list-search' -in @($event.focus.ancestry))
                $controlVerified = $focusMatches -and $samples.Count -gt 0 -and $spoken.Contains($expectedName)
                $response.control = @{ verified = [bool]$controlVerified; expectedName = $expectedName; speechSamples = $samples.Count; focus = $event.focus }
                $protocol.Add(@{ kind = 'focus-control-result'; trial = $trial; speechSamples = $samples.Count; verified = [bool]$controlVerified; expectedName = $expectedName; utc = [DateTime]::UtcNow.ToString('o') })
                if (-not $controlVerified) { throw 'Positive control did not match the intended Search focus and localized speech.' }
                # Retain the key request and a separately observed NVDA cancellation response.
                $lastCancellationUtc = $null
                $requestUtc = [DateTime]::UtcNow
                [TenantsSpeechDesktop]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)
                [TenantsSpeechDesktop]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero)
                $protocol.Add(@{ kind = 'interruption-control'; trial = $trial; utc = $requestUtc.ToString('o'); key = 'Control'; audibleCompletion = 'not inferred' })
                $responseDeadline = $requestUtc.AddSeconds(5)
                do {
                    Sample-Speech
                    if ($lastCancellationUtc -and $lastCancellationUtc -ge $requestUtc) { break }
                    Start-Sleep -Milliseconds 100
                } while ([DateTime]::UtcNow -lt $responseDeadline)
                $interruptionVerified = $lastCancellationUtc -and $lastCancellationUtc -ge $requestUtc
                $response.interruption = @{ verified = [bool]$interruptionVerified; requestedUtc = $requestUtc.ToString('o'); responseUtc = $(if ($interruptionVerified) { $lastCancellationUtc.ToString('o') } else { $null }) }
                $protocol.Add(@{ kind = 'interruption-control-result'; trial = $trial; verified = [bool]$interruptionVerified; requestedUtc = $requestUtc.ToString('o'); responseUtc = $response.interruption.responseUtc; status = $(if ($interruptionVerified) { 'observed cancellation response' } else { 'unverified; no matched response in bounded wait' }) })
                if (-not $interruptionVerified) { throw 'NVDA interruption response is unverified.' }
                $null = $verifiedControls.Add($activeTrial)
                $capture = $false
            }
            'complete' {
                Assert-CompletedCoverage @($event.completedTrials)
                $capture = $false; $metadata.completed = $true
                $metadata.completedTrials = @($event.completedTrials)
            }
            default { throw 'Unknown native capture protocol event.' }
        }
        $node.StandardInput.WriteLine(($response | ConvertTo-Json -Compress -Depth 8))
        $node.StandardInput.Flush()
        $progressDeadline = [DateTime]::UtcNow.AddMilliseconds($protocolProgressMilliseconds)
    }
    if (-not $node.WaitForExit(10000)) { throw 'Native runner did not exit after its output closed.' }
    $metadata.runnerExitCode = $node.ExitCode
    if ($node.ExitCode -ne 0) { throw 'Native fixture did not complete; retain its failure packet.' }
    if (-not $metadata.completed) { throw 'Native runner exited without complete protocol coverage.' }
    Assert-CompletedCoverage @($metadata.completedTrials)
    $metadata.passed = $true
}
catch {
    $metadata.failure = $_.Exception.Message
    Write-Warning $metadata.failure
}
finally {
    $capture = $false
    if ($nodeStarted) {
        try {
            if (-not $node.HasExited) {
                try { $node.StandardInput.Close() } catch { $metadata.inputCloseBlocker = $_.Exception.Message }
                if (-not $node.WaitForExit(5000)) {
                    & taskkill.exe /PID $node.Id /T /F | Out-Null
                    if (-not $node.WaitForExit(5000)) { throw 'Owned runner did not stop after tree cleanup.' }
                }
            }
        } catch { $metadata.runnerCleanupBlocker = $_.Exception.Message; $metadata.passed = $false }
        try {
            if ($stderr -and $stderr.Wait(5000)) {
                [IO.File]::WriteAllText((Join-Path $privateRoot 'runner.stderr'), $stderr.GetAwaiter().GetResult(), $encoding)
                $metadata.stderrRetained = $true
            } else { throw 'Owned runner stderr drain did not complete within its private bound.' }
        } catch {
            $metadata.stderrRetained = $false; $metadata.stderrBlocker = $_.Exception.Message; $metadata.passed = $false
            try { [IO.File]::WriteAllText((Join-Path $privateRoot 'runner.stderr-incomplete'), $metadata.stderrBlocker, $encoding) } catch { }
        }
    }
    foreach ($process in $ownedNvda) {
        try {
            $current = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($current -and $current.StartTime -eq $process.StartTime) { Stop-Process -Id $process.Id -ErrorAction Stop }
        } catch { $metadata.cleanupBlocker = 'The owned NVDA process could not be stopped by this token.'; $metadata.passed = $false }
    }
    try {
        $metadata.sourceSettingsUnchanged = if ($originalSettingsHash) { (Get-Sha256 $SettingsPath) -eq $originalSettingsHash } else { -not (Test-Path -LiteralPath $SettingsPath) }
        if (-not $metadata.sourceSettingsUnchanged) { throw 'Original NVDA settings changed during capture; the original file was not modified or restored by this collector.' }
    } catch { $metadata.settingsBlocker = $_.Exception.Message; $metadata.passed = $false }
    try {
        $effective = Join-Path $configRoot 'effective-settings.json'
        if (-not (Test-Path -LiteralPath $effective)) { throw 'Effective NVDA settings were not observed.' }
        $metadata.effectiveSettings = Get-Content -LiteralPath $effective -Raw | ConvertFrom-Json
    } catch { $metadata.settingsBlocker = $_.Exception.Message; $metadata.passed = $false }
    $metadata.finishedUtc = [DateTime]::UtcNow.ToString('o')
    $metadata.privateLogs = $privateRoot
    try { [IO.File]::WriteAllText((Join-Path $OutputDirectory 'speech.json'), (ConvertTo-Json -InputObject @($transcript.ToArray()) -Depth 8), $encoding) }
    catch { $metadata.speechWriteBlocker = $_.Exception.Message; $metadata.passed = $false }
    try { [IO.File]::WriteAllText((Join-Path $OutputDirectory 'timeline.json'), (ConvertTo-Json -InputObject @($protocol.ToArray()) -Depth 12), $encoding) }
    catch { $metadata.timelineWriteBlocker = $_.Exception.Message; $metadata.passed = $false }
    try {
        if ([TenantsSpeechDesktop]::GetForegroundWindow() -eq $browserHandle -and [TenantsSpeechDesktop]::IsWindow($entryForeground)) { $null = [TenantsSpeechDesktop]::SetForegroundWindow($entryForeground) }
    } catch { $metadata.foregroundRestoreBlocker = $_.Exception.Message; $metadata.passed = $false }
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'capture.json'), ($metadata | ConvertTo-Json -Depth 12), $encoding)
}
if (-not $metadata.passed) { exit 1 }
