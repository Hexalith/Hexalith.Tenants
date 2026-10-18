---
title: 'Contain malformed Unicode audit cursor and stream strings'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '66ab4a5b3f8152535ede44670b50466d24ef588b'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-651 records escaped unpaired-surrogate audit cursor and stream strings that throw during direct payload decoding instead of returning a validation failure. Cursor decoding still runs before category validation, so a malformed cursor throws even when the category is also malformed.

**Approach:** Return `InvalidEnvelope` for malformed Unicode cursor and stream decoding, before cursor-codec use or audit reads. Preserve authorization precedence, opaque cursor scope, stream selection, and every successfully decoded string. Prove both surrogate forms and mixed malformed payloads through the production query handler, and close only DW-651 after verification and independent review.

</frozen-after-approval>

## Implementation Notes

- Route facts: DW-650 evidence and the current parser settle the behavior. No user-visible intent gap or irreversible operation remains. The footprint is the two remaining `GetString()` calls in one private parser, the existing focused test class, this spec, evidence, and the selected backlog reconciliation. No new public API, dependency, or persistence change.
- Code map: `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs::DeserializeAuditPayload` decodes `cursor` at the string branch around line 316, before category, and `stream` at the string branch around line 373, after category and the from/to check. Both call `GetString()` with no `InvalidOperationException` catch. Category already catches that exception and returns bare `QueryAdapterFailureReason.InvalidEnvelope`. `GetTenantAuditQueryHandler.ExecuteCoreAsync` authorizes first, then parses, then checks `TenantAuditOwnership.IsValidSelector`, then `CursorCodec.TryDecode`, and only then reads audit state. Reuse that order unchanged. Do not change `TenantQueryPaginationPayloadParser`; it is a different query surface.
- Decode decision: catch only `InvalidOperationException` around each of those two `GetString()` calls and return bare `InvalidEnvelope` immediately, matching category. Do not treat a decode failure as an absent cursor or stream. Leave non-string cursors absent, non-string streams as the existing envelope failure, null/blank successfully decoded strings, last-duplicate JSON selection, date checks, category parsing, selector checks, and codec rejection unchanged.
- Tests: extend `tests/Hexalith.Tenants.Server.Tests/Queries/GetTenantAuditQueryHandlerTests.cs`. Reuse `ExecuteAsync`, `AssertOnlyAuthorityWasReadAsync`, `CreateStore`, `CreateCursorCodec`, and the real protected cursor. Cover escaped lone high and low surrogates (`\ud800`, `\udc00`) and the four raw malformed UTF-8 sequences already used for category (`80`, `C0 AF`, `ED A0 80`, `F4 90 80 80`) on cursor and on stream, for an authorized caller and a non-administrator. Cover mixed payloads: malformed Unicode category plus malformed cursor, and malformed Unicode category plus malformed stream. Cover a successfully decoded escaped `tenant` stream and a protected cursor whose JSON spelling uses equivalent escapes, including continuation across those spellings. Cover a valid surrogate pair that is not a protected cursor: it must reach `InvalidCursor` with no audit read.
- Acceptance: Given an authorized caller and a malformed Unicode cursor or stream, including a valid category, a malformed category, or a supplied ordinary invalid cursor beside a malformed stream, when the real handler executes, then it returns exactly `invalid-envelope`, no payload, no codec calls, and only the authority read. Given a non-administrator and the same payloads, when queried, then Forbidden still wins without audit reads. Given an escaped stream spelling of `tenant` or an equivalent escaped protected cursor, when queried, then existing row selection, cursor scope, and terminal paging are preserved. Given a decoded cursor that is not a protected cursor, when queried, then the existing invalid-cursor result is returned after codec use and before any audit read.
- Implementation order: add focused regressions and execute them against unchanged production; repair only cursor and stream decoding; rebuild and rerun the focused class and full Server project; independently review and recheck corrections; then reconcile DW-651 and the maintenance index while preserving all unrelated record bytes and historical evidence.
- Validation: use `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q`, then repository MTP `dotnet test --project ... -c Debug --no-build --no-restore` for focused and full Server lanes. Run `git diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-651-malformed-unicode-audit-cursor-stream.md`. Record exact commands, outcomes, and limitations in `dw-651-validation-evidence.md`.
- Scope constraints: preserve authorization precedence, cursor opacity, stream selection, tenant isolation, all DW-650 and DW-424 behavior, archive/sprint, dependency pointers, and existing release holds. Leave changes uncommitted and unstaged; this task does not require staging, branching, committing, or pushing.

- Completion: both `GetString()` calls now catch only `InvalidOperationException` and return bare `InvalidEnvelope`. The C# 14 `field` keyword required renaming the new theory-data variable to `propertyName`. Original focused execution failed 16/155; repaired Debug build has one transient MSB3026 warning and zero errors; focused 155/155 and full Server 1106/1106 pass with zero failures/skips. Independent review defers embedded audit-date surrogates as DW-652 and standard pagination cursors as DW-653. Exact commands remain in dw-651-validation-evidence.md.

## Review Triage Log

- Medium, defer (DW-652): embedded or swapped unpaired surrogates in audit `from`/`to` throw from `TryGetDateTimeOffset`. A saved `JsonElement` probe throws; whole-value lone surrogates still return false. This date path predates the cursor/stream catch.
- False: cursor and stream duplicate properties do not need a new selector. `TryGetProperty` already returns the last value, and the new catch applies only to that selected element.
- Low, rejected: the new theories name lone `\ud800`/`\udc00` and the four raw UTF-8 sequences required by the spec. Every other `GetString()` failure uses the same `InvalidOperationException` catch, so adding the category spelling matrix does not cover a separate failure mode.
- Low, rejected: system-owner stream routing is unchanged. Escaped `tenant` continuation fails unless the decoded stream matches the protected cursor scope.
- False: bare `InvalidEnvelope` is the specified result when cursor or stream decoding throws. A pending category or range diagnostic is returned only after both strings decode.
- Medium, patch: the planning notes still described the unfixed parser, and the required evidence file was absent at review time. The completion note and dw-651-validation-evidence.md record the landed catches and commands.
- Low, rejected: the catch comments match the existing category comment. Raw UTF-8 is already covered by handler tests; rewriting the comment does not change behavior.
- Medium, defer (DW-653): `TenantQueryPaginationPayloadParser.DeserializeStandardPayload` still calls cursor `GetString()` inside a `JsonException`-only catch. The spec leaves that parser unchanged, and `ExecuteAsync` rethrows the resulting `InvalidOperationException`.
