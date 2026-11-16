
# JSON is decoded before applying the same string policy; serialization owns delimiters.
function Write-PublicationJsonElement($Element, $Writer, $State) {
  switch ($Element.ValueKind.ToString()) {
    'Object' {
      $Writer.WriteStartObject()
      $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
      foreach ($property in $Element.EnumerateObject()) {
        $name = Normalize-ArtifactText $property.Name
        if ($name -cne $property.Name) { $State.Changed = $true }
        $unsafe = Test-UnsafeText $name
        if ($unsafe) { $State.Unsafe = $unsafe }
        if (!$names.Add($name)) { $State.Unsafe = 'duplicate normalized JSON property' }
        $Writer.WritePropertyName($name)
        Write-PublicationJsonElement $property.Value $Writer $State
      }
      $Writer.WriteEndObject()
    }
    'Array' {
      $Writer.WriteStartArray()
      foreach ($value in $Element.EnumerateArray()) { Write-PublicationJsonElement $value $Writer $State }
      $Writer.WriteEndArray()
    }
    'String' {
      $original = $Element.GetString()
      $normalized = Normalize-ArtifactText $original
      if ($normalized -cne $original) { $State.Changed = $true }
      $unsafe = Test-UnsafeText $normalized
      if ($unsafe) { $State.Unsafe = $unsafe }
      $Writer.WriteStringValue([string] $normalized)
    }
    default { $Element.WriteTo($Writer) }
  }
}

function Get-PublicationArtifactText([string] $Text, [switch] $Json) {
  if (!$Json) {
    $normalized = Normalize-ArtifactText $Text
    return [pscustomobject]@{ Text = $normalized; Changed = ($normalized -cne $Text); Unsafe = (Test-UnsafeText $normalized) }
  }
  $document = $null
  $stream = [System.IO.MemoryStream]::new()
  $writer = [System.Text.Json.Utf8JsonWriter]::new($stream)
  try {
    $options = [System.Text.Json.JsonDocumentOptions]::new()
    $options.MaxDepth = 100
    $document = [System.Text.Json.JsonDocument]::Parse($Text, $options)
    $state = [pscustomobject]@{ Changed = $false; Unsafe = $null }
    Write-PublicationJsonElement $document.RootElement $writer $state
    $writer.Flush()
    if ($state.Unsafe) { return [pscustomobject]@{ Text = $Text; Changed = $false; Unsafe = $state.Unsafe } }
    $normalized = if ($state.Changed) { [System.Text.Encoding]::UTF8.GetString($stream.ToArray()) } else { $Text }
    return [pscustomobject]@{ Text = $normalized; Changed = $state.Changed; Unsafe = $null }
  }
  catch { return [pscustomobject]@{ Text = $Text; Changed = $false; Unsafe = 'invalid JSON' } }
  finally {
    if ($null -ne $document) { $document.Dispose() }
    $writer.Dispose()
    $stream.Dispose()
  }
}
