param(
  [ValidateSet("happy-path", "error-handling")]
  [string] $Segment,
  [switch] $VerifyPublishedInputs
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "mutation-artifact-policy.ps1")

$reportRoot = Join-Path $RepoRoot "artifacts/mutation"

function Assert-PublicationDigestBinding([string] $SegmentRoot, [string] $Name) {
  $recordPath = Join-Path $SegmentRoot 'publication-digests.json'
  if (!(Test-Path -LiteralPath $recordPath -PathType Leaf)) { throw "Missing original publication capture for $Name." }
  $preparedRecord = Get-PublicationArtifactText (Get-Content -LiteralPath $recordPath -Raw) -Json
  if ($preparedRecord.Unsafe) { throw "Unsafe publication capture for $Name." }
  $record = $preparedRecord.Text | ConvertFrom-Json -Depth 100
  if ($record.schema -cne 'hexalith.mutation-publication.v1' -or $record.segment -cne $Name -or $record.capture -cne 'before-first-preparation') {
    throw "Invalid original publication capture for $Name."
  }
  $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
  foreach ($entry in $record.files) {
    $relative = [string] $entry.path
    $allowed = $relative -ceq 'tool-provenance.json' -or $relative -cmatch ('^(?:[^/:]+/)*reports/source-tools-' + [regex]::Escape($Name) + '\.(json|html)$')
    if (!$allowed -or $relative.Split('/') -contains '..' -or !$seen.Add($relative)) { throw 'Invalid publication capture path.' }
    if ($entry.originalSha256 -cnotmatch '^[0-9a-f]{64}$' -or $entry.publishedSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Invalid publication capture digest.' }
    $path = Join-Path $SegmentRoot $relative
    if (!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Attributes -band [System.IO.FileAttributes]::ReparsePoint) { throw 'Missing or linked captured publication file.' }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.publishedSha256) {
      throw "Published-side digest binding differs for $Name."
    }
  }
  $actual = @(Get-ChildItem -LiteralPath $SegmentRoot -Recurse -File | Where-Object {
    ($_.Directory.Name -ceq 'reports' -and $_.Name -cin @("source-tools-$Name.json", "source-tools-$Name.html")) -or $_.FullName -ceq (Join-Path $SegmentRoot 'tool-provenance.json')
  })
  if ($seen.Count -eq 0 -or $seen.Count -ne $actual.Count) { throw 'Publication capture file inventory differs.' }
  foreach ($file in $actual) {
    if (!$seen.Contains([System.IO.Path]::GetRelativePath($SegmentRoot, $file.FullName).Replace('\', '/'))) { throw 'Uncaptured publication file.' }
  }
  return $record
}

if ($VerifyPublishedInputs) {
  if ($Segment) { throw 'Published-input verification requires both fixed segments.' }
  $verified = @()
  foreach ($name in @('happy-path', 'error-handling')) {
    $verified += Assert-PublicationDigestBinding (Join-Path $reportRoot $name) $name
  }
  [pscustomobject]@{ schema = 'hexalith.mutation-publication-verification.v1'; segments = $verified } |
    ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $reportRoot 'publication-input-audit.json') -Encoding utf8
  Write-Host 'Both worker publication digest bindings verified.'
  return
}
foreach ($directory in @((Join-Path $RepoRoot "artifacts"), $reportRoot)) {
  if ((Test-Path -LiteralPath $directory) -and ((Get-Item -LiteralPath $directory).Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
    throw "Mutation publication refuses linked artifact directories."
  }
}
$segments = if ($Segment) { @($Segment) } else { @("happy-path", "error-handling") }
$files = New-Object System.Collections.Generic.List[System.IO.FileInfo]
foreach ($name in $segments) {
  $segmentRoot = Join-Path $reportRoot $name
  if (Test-Path -LiteralPath $segmentRoot) {
    if ((Get-Item -LiteralPath $segmentRoot).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
      throw "Mutation publication refuses linked segment directories."
    }
    $entries = @(Get-ChildItem -LiteralPath $segmentRoot -Recurse)
    if ($entries | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }) {
      throw "Mutation publication refuses linked artifacts and directories."
    }
    $entries | Where-Object {
        ([System.IO.Path]::GetFileName([System.IO.Path]::GetDirectoryName($_.FullName)) -ceq "reports" -and $_.Name -cin @("source-tools-$name.json", "source-tools-$name.html")) -or
        $_.FullName -cin @((Join-Path $segmentRoot "tool-provenance.json"), (Join-Path $segmentRoot "publication-digests.json"))
      } | ForEach-Object {
        if ($_.PSIsContainer) { throw "Mutation publication requires regular files at allowed artifact paths." }
        $files.Add($_)
      }
  }
}
if (!$Segment) {
  foreach ($name in @("job-summary.md", "mutation-report-audit.json", "publication-input-audit.json", "mutation-triage-admission.json", "mutation-validation-errors.txt")) {
    $path = Join-Path $reportRoot $name
    if (Test-Path -LiteralPath $path) {
      $item = Get-Item -LiteralPath $path
      if ($item.PSIsContainer) { throw "Mutation publication requires regular files at allowed artifact paths." }
      $files.Add($item)
    }
  }
}
if ($files.Count -eq 0) { throw "No allowed mutation artifacts are available for publication." }

# Preflight every selected file before changing any bytes or allowing an upload.
$prepared = New-Object System.Collections.Generic.List[object]
foreach ($file in $files) {
  if ($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
    throw "Mutation publication refuses linked artifacts."
  }
  $original = [string] (Get-Content -LiteralPath $file.FullName -Raw)
  $publication = Get-PublicationArtifactText $original -Json:($file.Extension -ieq ".json")
  $text = $publication.Text
  $unsafe = $publication.Unsafe
  if ($unsafe) { throw "Mutation artifact '$($file.Name)' failed publication scan: $unsafe." }
  $prepared.Add([pscustomobject]@{ Path = $file.FullName; Text = $text; Changed = ($text -cne $original); OriginalSha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() })
}

$existingCapture = $null
if ($Segment -and (Test-Path -LiteralPath (Join-Path (Join-Path $reportRoot $Segment) 'publication-digests.json'))) {
  $existingCapture = Assert-PublicationDigestBinding (Join-Path $reportRoot $Segment) $Segment
  if (@($prepared | Where-Object { $_.Changed }).Count -ne 0) { throw 'Existing publication capture cannot be recaptured after another normalization change.' }
}
foreach ($artifact in $prepared) {
  if ($artifact.Changed) {
    [System.IO.File]::WriteAllText($artifact.Path, $artifact.Text, [System.Text.UTF8Encoding]::new($false))
  }
}

if ($Segment -and $null -eq $existingCapture) {
  $segmentRoot = Join-Path $reportRoot $Segment
  $captured = @($prepared | ForEach-Object {
    [pscustomobject]@{ path = [System.IO.Path]::GetRelativePath($segmentRoot, $_.Path).Replace('\', '/'); originalSha256 = $_.OriginalSha256; publishedSha256 = (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash.ToLowerInvariant() }
  })
  $capture = [pscustomobject]@{ schema = 'hexalith.mutation-publication.v1'; segment = $Segment; capture = 'before-first-preparation'; capturedUtc = [DateTime]::UtcNow.ToString('O'); files = $captured }
  $captureText = $capture | ConvertTo-Json -Depth 100
  $capturePolicy = Get-PublicationArtifactText $captureText -Json
  if ($capturePolicy.Unsafe) { throw 'Unsafe publication capture metadata.' }
  [System.IO.File]::WriteAllText((Join-Path $segmentRoot 'publication-digests.json'), $captureText, [System.Text.UTF8Encoding]::new($false))
}
Write-Host "Mutation publication policy passed for $($files.Count) allowed artifacts."
