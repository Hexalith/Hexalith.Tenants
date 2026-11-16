
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
