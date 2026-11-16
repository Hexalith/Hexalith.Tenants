param(
  [ValidateSet("happy-path", "error-handling")]
  [string] $Segment
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "mutation-artifact-policy.ps1")

$reportRoot = Join-Path $RepoRoot "artifacts/mutation"
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
        $_.FullName -ceq (Join-Path $segmentRoot "tool-provenance.json")
      } | ForEach-Object {
        if ($_.PSIsContainer) { throw "Mutation publication requires regular files at allowed artifact paths." }
        $files.Add($_)
      }
  }
}
if (!$Segment) {
  foreach ($name in @("job-summary.md", "mutation-report-audit.json", "mutation-triage-admission.json", "mutation-validation-errors.txt")) {
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
  $text = Normalize-ArtifactText $original
  $unsafe = Test-UnsafeText $text
  if ($unsafe) { throw "Mutation artifact '$($file.Name)' failed publication scan: $unsafe." }
  $prepared.Add([pscustomobject]@{ Path = $file.FullName; Text = $text; Changed = ($text -cne $original) })
}
foreach ($artifact in $prepared) {
  if ($artifact.Changed) {
    $artifact.Text | Set-Content -LiteralPath $artifact.Path -Encoding utf8
  }
}
Write-Host "Mutation publication policy passed for $($files.Count) allowed artifacts."
