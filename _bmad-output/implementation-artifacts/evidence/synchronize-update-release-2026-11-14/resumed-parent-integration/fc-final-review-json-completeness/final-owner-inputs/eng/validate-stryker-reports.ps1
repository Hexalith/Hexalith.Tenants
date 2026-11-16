param(
  [string] $ManifestPath = "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json",
  [string] $ReportRoot = "artifacts/mutation",
  [string] $OutputPath = "artifacts/mutation/job-summary.md",
  [switch] $AllowMissingReports,
  [switch] $RequireCompatibilityProvenance
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "mutation-artifact-policy.ps1")
$ManifestFullPath = Join-Path $RepoRoot $ManifestPath
$ReportRootFullPath = Join-Path $RepoRoot $ReportRoot
$OutputFullPath = Join-Path $RepoRoot $OutputPath
$DotnetToolsManifestPath = Join-Path $RepoRoot ".config/dotnet-tools.json"
$StrykerToolVersion = "unknown"
if (Test-Path -LiteralPath $DotnetToolsManifestPath) {
  try {
    $resolvedVersion = (Get-Content -LiteralPath $DotnetToolsManifestPath -Raw | ConvertFrom-Json -Depth 100).tools.'dotnet-stryker'.version
    if (![string]::IsNullOrWhiteSpace([string] $resolvedVersion)) {
      $StrykerToolVersion = [string] $resolvedVersion
    }
  }
  catch {
    $StrykerToolVersion = "unknown"
  }
}
$OfficialStrykerBaseVersion = $StrykerToolVersion
$CompilerMetadataQualified = !$RequireCompatibilityProvenance -and $StrykerToolVersion -ceq "5.1.0"
$errors = New-Object System.Collections.Generic.List[string]
$summaryLines = New-Object System.Collections.Generic.List[string]
$reportAudits = New-Object System.Collections.Generic.List[object]

function Add-Failure([string] $Message) {
  $errors.Add($Message) | Out-Null
}

function ConvertTo-RepoPath([string] $Path) {
  $full = [System.IO.Path]::GetFullPath($Path)
  $root = [System.IO.Path]::GetFullPath($RepoRoot)
  if (!$full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Path escapes repository root: $Path"
  }

  return $full.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Replace('\', '/')
}

function Read-Json([string] $Path) {
  Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 100
}

function Get-PSObjectPropertyValue($Object, [string] $Name) {
  # Set-StrictMode -Version Latest throws on direct dot-access to a property that
  # does not exist on a PSCustomObject; this looks the property up defensively so a
  # genuinely missing/malformed JSON field is reported as a validation failure
  # instead of crashing the script.
  if ($null -eq $Object) { return $null }
  $property = $Object.PSObject.Properties[$Name]
  if ($null -eq $property) { return $null }
  return $property.Value
}

function Get-JsonValuesByName($Node, [string] $Name) {
  $values = New-Object System.Collections.Generic.List[object]
  if ($null -eq $Node) { return $values }

  if ($Node -is [System.Management.Automation.PSCustomObject]) {
    foreach ($property in $Node.PSObject.Properties) {
      if ($property.Name -eq $Name) {
        $values.Add($property.Value) | Out-Null
      }

      foreach ($child in Get-JsonValuesByName $property.Value $Name) {
        $values.Add($child) | Out-Null
      }
    }
  }
  elseif ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) {
    foreach ($item in $Node) {
      foreach ($child in Get-JsonValuesByName $item $Name) {
        $values.Add($child) | Out-Null
      }
    }
  }

  return $values
}

function Test-WildcardRepoPath([string] $Path, [string] $Pattern) {
  $normalizedPattern = $Pattern.Replace('\', '/')
  return $Path -like $normalizedPattern
}

function Test-ExplicitlyExcluded([string] $Path, $Exclusions) {
  foreach ($exclusion in @($Exclusions)) {
    if (Test-WildcardRepoPath $Path ([string] $exclusion.path)) {
      if ([string]::IsNullOrWhiteSpace([string] $exclusion.reason) -or [string]::IsNullOrWhiteSpace([string] $exclusion.owner)) {
        Add-Failure "Exclusion '$($exclusion.path)' must include reason and owner."
      }

      return $true
    }
  }

  return $false
}

function Get-MutatedFiles($Report) {
  $items = New-Object System.Collections.Generic.List[object]

  if ($null -ne $Report.files -and $Report.files -is [System.Management.Automation.PSCustomObject]) {
    foreach ($property in $Report.files.PSObject.Properties) {
      $mutants = @(Get-PSObjectPropertyValue $property.Value "mutants")
      $statuses = New-Object System.Collections.Generic.List[string]
      $liveStatuses = New-Object System.Collections.Generic.List[string]
      $invalidCompilerIds = New-Object System.Collections.Generic.List[string]
      foreach ($mutant in $mutants) {
        $status = [string] (Get-PSObjectPropertyValue $mutant "status")
        $id = [string] (Get-PSObjectPropertyValue $mutant "id")
        $statuses.Add($status) | Out-Null
        if (@("Killed", "Survived", "Timeout", "CompileError", "Ignored", "NoCoverage") -cnotcontains $status) {
          Add-Failure "Mutation report has unknown or unhandled status '$status' for '$($property.Name)' mutant '$id'."
        }

        $invalidCompilerCandidate = $false
        if ($status -ceq "CompileError") {
          $coveredBy = $mutant.PSObject.Properties["coveredBy"]
          $killedBy = $mutant.PSObject.Properties["killedBy"]
          $reason = Get-PSObjectPropertyValue $mutant "statusReason"
          # Pinned Stryker 5.1 marks rollback candidates final BEFORE file filtering.
          # They were never executable. Accept only its exact nonexecuted shape;
          # preserve the report status and refuse evidence of execution or drift.
          $invalidCompilerCandidate = $CompilerMetadataQualified `
            -and $id -cmatch '^\d+$' `
            -and $reason -is [string] -and $reason -ceq "Mutant caused compile errors" `
            -and $null -ne $coveredBy -and $coveredBy.Value -is [System.Array] -and $coveredBy.Value.Count -eq 0 `
            -and $null -ne $killedBy -and $killedBy.Value -is [System.Array] -and $killedBy.Value.Count -eq 0
          if ($invalidCompilerCandidate) {
            $invalidCompilerIds.Add($id) | Out-Null
          }
          else {
            Add-Failure "Unqualified CompileError metadata for '$($property.Name)' mutant '$id'; pinned reason and present empty coverage/kills are required."
          }
        }
        if ($status -cne "Ignored" -and !$invalidCompilerCandidate) {
          $liveStatuses.Add($status) | Out-Null
        }
      }

      $items.Add([pscustomobject]@{
        Path = ([string] $property.Name).Replace('\', '/')
        Statuses = $statuses.ToArray()
        LiveStatuses = $liveStatuses.ToArray()
        InvalidCompilerIds = $invalidCompilerIds.ToArray()
      }) | Out-Null
    }
  }

  return $items
}

function Get-TextSha256([string] $Text) {
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
  return [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-TriageCount($TriageEntries, [string] $SegmentName, [string] $Status) {
  $count = 0
  foreach ($entry in @($TriageEntries)) {
    if ([string] $entry.segment -ne $SegmentName -or [string] $entry.status -ne $Status) {
      continue
    }

    if ([string]::IsNullOrWhiteSpace([string] $entry.action) -or @($manifest.triageActions) -notcontains [string] $entry.action) {
      Add-Failure "Problem-mutant triage for segment '$SegmentName' status '$Status' has invalid action '$($entry.action)'."
    }

    if ([string]::IsNullOrWhiteSpace([string] $entry.owner) -or [string]::IsNullOrWhiteSpace([string] $entry.rationale)) {
      Add-Failure "Problem-mutant triage for segment '$SegmentName' status '$Status' must include owner and rationale."
    }

    $count += [int] $entry.count
  }

  return $count
}

if (!(Test-Path -LiteralPath $ManifestFullPath)) {
  throw "Missing mutation target manifest: $ManifestPath"
}

$manifest = Read-Json $ManifestFullPath
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFullPath) | Out-Null

$approvedRoots = @($manifest.approvedTargetRoots | ForEach-Object { [string] $_ })
$segments = @($manifest.segments)
$explicitExclusions = @($manifest.explicitExclusions)
$problemMutantTriage = @($manifest.problemMutantTriage)
$allTargetFiles = New-Object System.Collections.Generic.HashSet[string]
$reportedTargetFiles = New-Object System.Collections.Generic.HashSet[string]
$combinedConfigText = ""
$hasAllRequiredReports = $true

foreach ($root in $approvedRoots) {
  $fullRoot = Join-Path $RepoRoot $root
  if (!(Test-Path -LiteralPath $fullRoot)) {
    Add-Failure "Approved target root missing: $root"
    continue
  }

  Get-ChildItem -LiteralPath $fullRoot -Recurse -File -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(g|generated)\.cs$' } |
    ForEach-Object { [void] $allTargetFiles.Add((ConvertTo-RepoPath $_.FullName)) }
}

foreach ($segment in $segments) {
  $toolProvenance = $null
  $segmentReportRoot = $ReportRootFullPath
  if ($RequireCompatibilityProvenance) {
    $CompilerMetadataQualified = $false
    $expectedRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "artifacts/mutation"))
    if ([System.IO.Path]::GetFullPath($ReportRootFullPath) -cne $expectedRoot) {
      Add-Failure "Compatibility provenance requires the exact repository mutation output root."
    }
    else {
      try {
        $verifiedText = & python3 (Join-Path $RepoRoot "eng/stryker_compat.py") verify ([string] $segment.name)
        if ($LASTEXITCODE -ne 0) { throw "Private tool verification refused." }
        $toolProvenance = ($verifiedText -join "`n") | ConvertFrom-Json -Depth 100
        $StrykerToolVersion = [string] $toolProvenance.build.informational_version
        $CompilerMetadataQualified = $true
      }
      catch {
        Add-Failure "Missing or invalid exact private-tool provenance for segment '$($segment.name)'."
      }
    }
    $segmentReportRoot = Join-Path $ReportRootFullPath ([string] $segment.name)
  }

  $configPath = Join-Path $RepoRoot ([string] $segment.config)
  if (!(Test-Path -LiteralPath $configPath)) {
    Add-Failure "Missing Stryker config for segment '$($segment.name)': $($segment.config)"
    $hasAllRequiredReports = $false
    continue
  }

  $combinedConfigText += "`n" + (Get-Content -LiteralPath $configPath -Raw)
  $config = Read-Json $configPath
  $strykerConfigSection = Get-PSObjectPropertyValue $config "stryker-config"
  $projectDirectory = (Split-Path -Parent ([string] (Get-PSObjectPropertyValue $strykerConfigSection "project"))).Replace('\', '/')
  foreach ($field in @("project", "configuration", "reporters", "coverage-analysis", "thresholds", "mutate")) {
    if ($null -eq (Get-PSObjectPropertyValue $strykerConfigSection $field)) {
      Add-Failure "Stryker config '$($segment.config)' is missing '$field'."
    }
  }

  if ($null -ne (Get-PSObjectPropertyValue $strykerConfigSection "solution")) {
    Add-Failure "Stryker config '$($segment.config)' must not set 'solution' — mutation is project-scoped and must not build the umbrella .slnx."
  }

  $configuredReporters = @(Get-PSObjectPropertyValue $strykerConfigSection "reporters")
  foreach ($reporter in @("progress", "html", "json")) {
    if ($configuredReporters -notcontains $reporter) {
      Add-Failure "Stryker segment '$($segment.name)' does not include reporter '$reporter'."
    }
  }

  $breakThreshold = Get-PSObjectPropertyValue (Get-PSObjectPropertyValue $strykerConfigSection "thresholds") "break"
  if ($null -eq $breakThreshold -or [int] $breakThreshold -lt [int] $segment.threshold) {
    Add-Failure "Stryker segment '$($segment.name)' break threshold is below manifest threshold $($segment.threshold)."
  }

  $jsonReport = Get-ChildItem -LiteralPath $segmentReportRoot -Recurse -File -Filter "*.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*$($segment.artifactPrefix)*" } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

  if ($null -eq $jsonReport) {
    if ($AllowMissingReports) {
      $summaryLines.Add("- $($segment.name): report missing (allowed for local config validation only)") | Out-Null
      $hasAllRequiredReports = $false
      continue
    }

    Add-Failure "Missing JSON mutation report for segment '$($segment.name)' under $ReportRoot."
    $hasAllRequiredReports = $false
    continue
  }

  $inputSha256 = (Get-FileHash -LiteralPath $jsonReport.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  $rawReportText = Get-Content -LiteralPath $jsonReport.FullName -Raw
  $publication = Get-PublicationArtifactText $rawReportText -Json
  $reportText = $publication.Text
  $reportText | Set-Content -LiteralPath $jsonReport.FullName -Encoding utf8
  $unsafe = $publication.Unsafe
  if ($unsafe) {
    Add-Failure "Mutation report '$($jsonReport.Name)' failed redaction scan: $unsafe."
  }

  try {
    $report = $reportText | ConvertFrom-Json -Depth 100
    $rawReport = $rawReportText | ConvertFrom-Json -Depth 100
  }
  catch {
    Add-Failure "Mutation report '$($jsonReport.Name)' is malformed JSON: $($_.Exception.Message)"
    $hasAllRequiredReports = $false
    continue
  }

  $mutatedFiles = @(Get-MutatedFiles $report)
  if ($mutatedFiles.Count -eq 0) {
    Add-Failure "Mutation report '$($jsonReport.Name)' contains zero mutated source files."
  }

  foreach ($file in $mutatedFiles) {
    $repoPath = [string] $file.Path
    if ([System.IO.Path]::IsPathRooted($repoPath)) {
      $repoPath = ConvertTo-RepoPath $repoPath
    }
    else {
      $repoPath = $repoPath.TrimStart('.', '/', '\')
      $isRepoRelative = $repoPath.StartsWith("src/", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $repoPath.StartsWith("tests/", [System.StringComparison]::OrdinalIgnoreCase)
      if (!$isRepoRelative -and ![string]::IsNullOrWhiteSpace($projectDirectory)) {
        $repoPath = "$projectDirectory/$repoPath"
      }
    }

    $inScope = $false
    foreach ($root in $approvedRoots) {
      if ($repoPath.StartsWith($root.TrimEnd('/') + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
        $inScope = $true
      }
    }

    $activeStatuses = @($file.Statuses | Where-Object { $_ -ne "Ignored" })
    if (!$inScope -and @($file.LiveStatuses).Count -gt 0) {
      Add-Failure "Mutation report '$($jsonReport.Name)' includes out-of-scope file '$repoPath'."
    }

    if ($inScope -and $activeStatuses.Count -gt 0) {
      [void] $reportedTargetFiles.Add($repoPath)
    }
  }

  $statuses = @($mutatedFiles | ForEach-Object { $_.Statuses })
  $invalidCompilerCount = @($mutatedFiles | ForEach-Object { $_.InvalidCompilerIds }).Count
  $mutantCount = @($mutatedFiles | ForEach-Object { $_.LiveStatuses }).Count
  if ($mutantCount -eq 0) {
    Add-Failure "Mutation report '$($jsonReport.Name)' contains zero mutants."
  }

  $minimumMutantCount = if ($null -ne $segment.minimumMutantCount) { [int] $segment.minimumMutantCount } else { 1 }
  if ($mutantCount -lt $minimumMutantCount) {
    Add-Failure "Mutation report '$($jsonReport.Name)' mutant count $mutantCount is below manifest baseline $minimumMutantCount for segment '$($segment.name)'."
  }

  $survived = @($statuses | Where-Object { $_ -eq "Survived" }).Count
  $noCoverage = @($statuses | Where-Object { $_ -eq "NoCoverage" }).Count
  $timeout = @($statuses | Where-Object { $_ -eq "Timeout" }).Count
  $compileError = @($statuses | Where-Object { $_ -eq "CompileError" }).Count
  foreach ($problem in @(
      @{ Status = "Survived"; Count = $survived },
      @{ Status = "NoCoverage"; Count = $noCoverage },
      @{ Status = "Timeout"; Count = $timeout },
      @{ Status = "CompileError"; Count = ($compileError - $invalidCompilerCount) }
    )) {
    if ($problem.Count -gt (Get-TriageCount $problemMutantTriage ([string] $segment.name) ([string] $problem.Status))) {
      Add-Failure "Mutation report '$($jsonReport.Name)' has untriaged $($problem.Status) mutants in segment '$($segment.name)': count=$($problem.Count)."
    }
  }

  $rawSources = @()
  $rawStatuses = @()
  foreach ($property in $rawReport.files.PSObject.Properties) {
    $source = Get-PSObjectPropertyValue $property.Value "source"
    if ($source -is [string]) {
      $rawSources += [pscustomobject]@{ Path = Normalize-ArtifactText $property.Name; SourceSha256 = Get-TextSha256 $source }
    }
    foreach ($mutant in @(Get-PSObjectPropertyValue $property.Value "mutants")) {
      $rawStatuses += [pscustomobject]@{
        Path = Normalize-ArtifactText $property.Name
        Id = [string] (Get-PSObjectPropertyValue $mutant "id")
        Status = [string] (Get-PSObjectPropertyValue $mutant "status")
      }
    }
  }
  $reportAudits.Add([pscustomobject]@{
    Report = ConvertTo-RepoPath $jsonReport.FullName
    InputSha256 = $inputSha256
    ToolProvenance = $toolProvenance
    OutputSha256 = ""
    RawMutantStatuses = $rawStatuses
    RawSourceHashes = $rawSources
    LiveMutantCount = $mutantCount
    InvalidCompilerCandidates = @($mutatedFiles | ForEach-Object {
      $path = $_.Path
      foreach ($id in $_.InvalidCompilerIds) { [pscustomobject]@{ Path = $path; Id = $id } }
    })
    StatusCounts = @($statuses | Group-Object -CaseSensitive | Sort-Object Name | ForEach-Object {
      [pscustomobject]@{ Status = $_.Name; Count = $_.Count }
    })
  }) | Out-Null
  $summaryLines.Add("- $($segment.name): report $(ConvertTo-RepoPath $jsonReport.FullName), liveMutants=$mutantCount, survived=$survived, noCoverage=$noCoverage, timeout=$timeout, compileError=$compileError (nonexecuted invalid candidates=$invalidCompilerCount)") | Out-Null
}

if (Test-Path -LiteralPath $ReportRootFullPath) {
  $textExtensions = @(".json", ".html", ".htm", ".md", ".txt", ".xml", ".log", ".js", ".css")
  Get-ChildItem -LiteralPath $ReportRootFullPath -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $textExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
      $publication = Get-PublicationArtifactText (Get-Content -LiteralPath $_.FullName -Raw) -Json:($_.Extension -ieq ".json")
      $artifactText = $publication.Text
      $artifactText | Set-Content -LiteralPath $_.FullName -Encoding utf8
      $unsafe = $publication.Unsafe
      if ($unsafe) {
        Add-Failure "Mutation artifact '$(ConvertTo-RepoPath $_.FullName)' failed redaction scan: $unsafe."
      }
    }
}

foreach ($audit in $reportAudits) {
  $audit.OutputSha256 = (Get-FileHash -LiteralPath (Join-Path $RepoRoot $audit.Report) -Algorithm SHA256).Hash.ToLowerInvariant()
}
$auditPath = Join-Path (Split-Path -Parent $OutputFullPath) "mutation-report-audit.json"
[pscustomobject]@{ StrykerVersion = $StrykerToolVersion; OfficialBaseVersion = $OfficialStrykerBaseVersion; CompatibilityProvenanceRequired = [bool] $RequireCompatibilityProvenance; Reports = $reportAudits.ToArray() } |
  ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $auditPath -Encoding utf8

if ($hasAllRequiredReports) {
  foreach ($targetFile in $allTargetFiles) {
    if (!$reportedTargetFiles.Contains($targetFile) -and !(Test-ExplicitlyExcluded $targetFile $explicitExclusions)) {
      Add-Failure "Target drift: '$targetFile' did not appear in mutation reports and is not explicitly excluded with rationale."
    }
  }
}

$summary = @(
  "## Mutation Evidence",
  "",
  "- Stryker tool: $StrykerToolVersion (official base pin $OfficialStrykerBaseVersion; exact private provenance required=$([bool] $RequireCompatibilityProvenance))",
  "- Target manifest: $ManifestPath",
  "- Approved roots: $($approvedRoots -join ', ')",
  "- Target file count: $($allTargetFiles.Count)",
  "- Submodules: root-level checkout only; no recursive nested submodule command is used",
  "",
  "### Segments"
) + $summaryLines

$summary | Set-Content -LiteralPath $OutputFullPath -Encoding utf8

if ($errors.Count -gt 0) {
  $failurePath = Join-Path (Split-Path -Parent $OutputFullPath) "mutation-validation-errors.txt"
  $errors | Set-Content -LiteralPath $failurePath -Encoding utf8
  throw "Mutation validation failed:`n - $($errors -join "`n - ")"
}

Write-Host "Mutation validation passed. Summary: $OutputPath"
