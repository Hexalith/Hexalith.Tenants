param(
  [ValidateSet("happy-path", "error-handling")]
  [string] $Segment
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "mutation-artifact-policy.ps1")

$reportRoot = Join-Path $RepoRoot "artifacts/mutation"
$segments = if ($Segment) { @($Segment) } else { @("happy-path", "error-handling") }
$files = New-Object System.Collections.Generic.List[System.IO.FileInfo]
foreach ($name in $segments) {
  $segmentRoot = Join-Path $reportRoot $name
  if (Test-Path -LiteralPath $segmentRoot) {
    if ((Get-Item -LiteralPath $segmentRoot).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
      throw "Mutation publication refuses linked segment directories."
    }
    Get-ChildItem -LiteralPath $segmentRoot -Recurse -File |
      Where-Object {
        ($_.Directory.Name -ceq "reports" -and $_.Name -cin @("source-tools-$name.json", "source-tools-$name.html")) -or
        $_.FullName -ceq (Join-Path $segmentRoot "tool-provenance.json")
      } | ForEach-Object { $files.Add($_) }
  }
}
if (!$Segment) {
  foreach ($name in @("job-summary.md", "mutation-report-audit.json", "mutation-validation-errors.txt")) {
    $path = Join-Path $reportRoot $name
    if (Test-Path -LiteralPath $path -PathType Leaf) {
      $files.Add((Get-Item -LiteralPath $path))
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
  $text = Normalize-ArtifactText (Get-Content -LiteralPath $file.FullName -Raw)
  $unsafe = Test-UnsafeText $text
  if ($unsafe) { throw "Mutation artifact '$($file.Name)' failed publication scan: $unsafe." }
  $prepared.Add([pscustomobject]@{ Path = $file.FullName; Text = $text })
}
foreach ($artifact in $prepared) {
  $artifact.Text | Set-Content -LiteralPath $artifact.Path -Encoding utf8
}
Write-Host "Mutation publication policy passed for $($files.Count) allowed artifacts."
