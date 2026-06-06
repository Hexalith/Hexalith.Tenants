export function focusCorrectionLauncher(auditReference) {
  if (!auditReference) {
    return false;
  }

  const candidates = document.querySelectorAll('[data-correction-focus-reference], [data-audit-reference]');
  for (const candidate of candidates) {
    const correctionReference = candidate.getAttribute('data-correction-focus-reference');
    const auditRowReference = candidate.getAttribute('data-audit-reference');
    if (correctionReference === auditReference || auditRowReference === auditReference) {
      if (!candidate.hasAttribute('tabindex')) {
        candidate.setAttribute('tabindex', '-1');
      }

      candidate.focus({ preventScroll: false });
      return true;
    }
  }

  return false;
}
