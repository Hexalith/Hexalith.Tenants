## Other findings

- Historical `eng/prepare-mutation-artifacts.ps1:55` scans serialized JSON without decoding escapes. A report containing `{"source":"password\u003dsynthetic"}` exits successfully and retains the credential marker. The supplied publication controls pass despite this. Current owner coverage addresses it in `testJsonEscapedCredentialIsDecodedAndRefused` at `tests/eng/test_mutation_artifact_publication.py:201`.

- Historical normalization at `eng/prepare-mutation-artifacts.ps1:55` corrupts JSON when a home path ends a string: `{"source":"/home/private-person"}` becomes `{"source":"<local-user>` plus a newline, yet publication succeeds. Current owner coverage addresses this in `testJsonTerminalHomePathKeepsDelimitersAndIdempotentBytes` at `tests/eng/test_mutation_artifact_publication.py:217`.

- Historical `eng/prepare-mutation-artifacts.ps1:62` unconditionally appends a newline on every preparation. Repeating publication changed `{"status":"Killed"}\n` into `{"status":"Killed"}\n\n`, invalidating report hashes recorded before aggregate preparation. The supplied controls assert parsed values rather than byte stability. Current owner coverage addresses this in `testActualValidatorAuditHashSurvivesIdempotentPublication` at `tests/eng/test_mutation_artifact_publication.py:161`.

These defects were reproduced using the historical scripts in a disposable workspace. Current owner code and assertions address all three; they are not remaining gaps in present source.
