param([string] $OwnerRoot)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$manifest = Get-Content -LiteralPath (Join-Path $OwnerRoot 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json') -Raw | ConvertFrom-Json -Depth 100
$errors = New-Object System.Collections.Generic.List[string]
function Add-Failure([string] $Message) { $errors.Add($Message) | Out-Null }
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $OwnerRoot 'eng/validate-stryker-reports.ps1'), [ref] $tokens, [ref] $parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Actual validator has syntax errors.' }
$function = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-TriageCount' }, $true))
if ($function.Count -ne 1) { throw 'Expected the one exact existing count-policy function.' }
Invoke-Expression $function[0].Extent.Text
$counts = New-Object System.Collections.Generic.List[object]
foreach ($segment in @('happy-path', 'error-handling')) {
  foreach ($status in @('Survived', 'NoCoverage', 'Timeout')) {
    $actual = Get-TriageCount $manifest.problemMutantTriage $segment $status
    $expected = @($manifest.problemMutantTriage | Where-Object { $_.segment -ceq $segment -and $_.status -ceq $status } | Measure-Object -Property count -Sum).Sum
    if ($actual -ne $expected) { throw 'Actual count-policy maximum differs.' }
    $counts.Add([pscustomobject]@{ Segment = $segment; Status = $status; Maximum = $actual })
  }
}
if ($errors.Count -ne 0) { throw 'Finite real-role/action/rationale entries refused by actual policy.' }
$entry = $manifest.problemMutantTriage[0] | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
$entry.action = 'unreviewed-action'
$null = Get-TriageCount @($entry) $entry.segment $entry.status
if ($errors.Count -eq 0) { throw 'Actual policy failed to refuse an invalid action.' }
$errors.Clear()
$entry.action = 'deferred-with-owner'
$entry.owner = ''
$null = Get-TriageCount @($entry) $entry.segment $entry.status
if ($errors.Count -eq 0) { throw 'Actual policy failed to refuse a missing owner.' }
[pscustomobject]@{ Scope = 'Actual existing aggregate-count contract only; no complete/native report qualification'; Counts = $counts.ToArray(); InvalidActionRefused = $true; MissingOwnerRefused = $true } | ConvertTo-Json -Depth 100
