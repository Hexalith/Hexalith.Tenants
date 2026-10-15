// Fluent 5 initializes the native combobox name once, from its visible field label.
// Keep the existing control (and its focus/value) when the resolved search scope changes.
export function syncStatusFilterLabel() {
  const dropdown = document.querySelector('[data-testid="tenants-list-status-filter"]');
  const control = dropdown?.querySelector('button[slot="control"]');
  const label = dropdown?.getAttribute('aria-label');
  if (!control || !label) return false;
  control.setAttribute('aria-label', label);
  return true;
}
