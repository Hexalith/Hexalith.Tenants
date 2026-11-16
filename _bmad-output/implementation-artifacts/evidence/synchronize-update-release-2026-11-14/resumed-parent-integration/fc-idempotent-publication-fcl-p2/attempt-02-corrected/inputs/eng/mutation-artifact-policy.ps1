# Shared private publication policy; function bodies are unchanged from the strict validator.
function Normalize-ArtifactText([string] $Text) {
  $normalizedRoot = [regex]::Escape($RepoRoot)
  $text = [regex]::Replace($Text, $normalizedRoot, ".", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $text = [regex]::Replace($text, 'C:\\Users\\[^\\]+', '<local-user>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $text = [regex]::Replace($text, '/home/[^/\s]+|/Users/[^/\s]+', '<local-user>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  return $text
}

function Test-UnsafeText([string] $Text) {
  if ([string]::IsNullOrEmpty($Text)) { return $null }
  if ($Text -match 'C:\\Users\\|/home/[^/\s]+|/Users/[^/\s]+') { return "machine-local path" }
  # A one-character JavaScript object reference is code, not a credential literal.
  # Keep quoted values and standalone markers under the ordinary strict scan.
  $credentialText = [regex]::Replace($Text, '(?<prefix>[{,]\s*)[Tt]oken\s*:\s*[A-Za-z_$](?=\s*[},])', '${prefix}token-reference')
  # A C# token variable initialized from another variable's .Token property
  # contains no literal credential value. Preserve all ordinary/prefixed markers.
  $credentialText = [regex]::Replace($credentialText, 'var\s+[A-Za-z_][A-Za-z0-9_]*Token\s*=\s*[A-Za-z_][A-Za-z0-9_]*\.Token\s*;', 'cancellation-reference')
  if ($credentialText -match '(?i)(bearer\s+[a-z0-9._-]{12,}|api[_-]?key\s*[:=]|password\s*[:=]|secret\s*[:=]|(?<!cancellation)token\s*(?::|=(?!>|\\u003e)))') { return "secret-like value" }
  if ($Text -match '(?i)tenant[_-]?id\s*[:=]\s*["'']?[0-9a-f-]{16,}') { return "tenant identifier" }
  return $null
}
