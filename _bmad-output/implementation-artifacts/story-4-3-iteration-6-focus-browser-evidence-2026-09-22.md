# Story 4.3 Iteration 6 — Real Chromium Focus Evidence

## Validator

- Command: `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`
- Browser: Google Chrome 148.0.7778.215
- Dependency shape: system Chromium/Chrome, Python standard-library loopback server, and the UI project's already-restored Fluent UI browser module; no new package, submodule, or AppHost dependency.
- Shipped module under test: `src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js`
- Rendered target: the shipped Fluent UI `fluent-button` custom element carrying the page's exact Cancel ID.

## Result

The validator passed the shipped module in real headless Chromium:

```text
PASS start-to-exact-cancel:tenants-global-admin-remove-cancel-button;existing-unfocusable-cancel-to-acknowledgement:remove-acknowledgement;missing-cancel-to-acknowledgement:remove-acknowledgement;missing-preview-to-lifecycle:remove-lifecycle
```

This proves that the start sentinel moved `document.activeElement` to the exact rendered Cancel target, a rendered but disabled Cancel target and a missing Cancel target both routed to the acknowledgement input, and a missing preview routed to the visible lifecycle region.

## Mutation Sensitivity

The runner replaced only a temporary copy of `focusElementById` with `return true;` and required the Chromium validation to fail. Chromium reported:

```text
FAIL start-to-exact-cancel expected tenants-global-admin-remove-cancel-button but focused remove-focus-start;start-to-exact-cancel:remove-focus-start
```

The repeatable validator therefore cannot pass on a mocked Boolean result without real focus movement.

## CI Enforcement

`.github/workflows/story-guards.yml` runs the validator in the required
`validate-remove-focus-in-chromium` job after a root-only submodule initialization and a
warning-clean Release build of the UI test project. The workflow passed `actionlint` locally.
