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

export function focusElementById(elementId) {
  if (!elementId) {
    return false;
  }

  const target = document.getElementById(elementId);
  if (!target) {
    return false;
  }

  target.focus({ preventScroll: false });
  return document.activeElement === target;
}

export function focusAuditLauncher(focusId, originTestId, headingId) {
  return new Promise(resolve => {
    const selector = `[data-testid="${originTestId}"]`;
    const initialUrl = window.location.href;
    let observer;
    let routeInterval;
    let finished = false;

    function isTerminal(element, attribute) {
      const value = element?.getAttribute(attribute);
      return value != null && value.toLowerCase() !== 'false';
    }

    function finish(found) {
      if (finished) {
        return;
      }
      finished = true;
      observer?.disconnect();
      window.clearInterval(routeInterval);
      window.removeEventListener('pagehide', leaveRoute);
      window.removeEventListener('popstate', leaveRoute);
      window.removeEventListener('hashchange', leaveRoute);
      resolve(found);
    }

    function leaveRoute() {
      finish(false);
    }

    function check() {
      if (window.location.href !== initialUrl) {
        finish(false);
        return;
      }

      const origin = document.querySelector(selector);
      if (!origin) {
        const page = Array.from(document.querySelectorAll('[data-audit-focus-heading]'))
          .find(candidate => candidate.getAttribute('data-audit-focus-heading') === headingId);
        const pageSettled = page && (page.getAttribute('data-audit-focus-active-origin') !== originTestId
          || isTerminal(page, 'data-audit-focus-page-terminal'));
        if (pageSettled) {
          const heading = document.getElementById(headingId);
          if (heading) {
            if (!heading.hasAttribute('tabindex')) {
              heading.setAttribute('tabindex', '-1');
            }
            heading.focus({ preventScroll: false });
          }
          finish(false);
        }
        return;
      }

      const anchor = document.getElementById(focusId);
      if (anchor && origin.contains(anchor)) {
        const row = anchor.closest('fluent-data-grid-row, [role="row"], tr');
        const header = anchor.closest('[data-testid="tenants-detail-identity"]');
        const container = row || header || anchor;
        const launcher = container.querySelector('[data-testid="tenants-audit-entrypoint"]');
        if (launcher?.tagName.toLowerCase() === 'fluent-anchor-button'
            && launcher.getAttribute('href')) {
          launcher.focus({ preventScroll: false });
          if (document.activeElement === launcher) {
            finish(true);
            return;
          }
        }
      }

      if (isTerminal(origin, 'data-audit-focus-terminal')) {
        const heading = document.getElementById(headingId)
          || origin.querySelector('[data-testid="tenants-detail-state-header"] h1');
        if (heading) {
          if (!heading.hasAttribute('tabindex')) {
            heading.setAttribute('tabindex', '-1');
          }
          heading.focus({ preventScroll: false });
        }
        finish(false);
      }
    }

    observer = new MutationObserver(check);
    routeInterval = window.setInterval(() => {
      if (window.location.href !== initialUrl) {
        finish(false);
      }
    }, 250);
    window.addEventListener('pagehide', leaveRoute, { once: true });
    window.addEventListener('popstate', leaveRoute);
    window.addEventListener('hashchange', leaveRoute);
    observer.observe(document, { childList: true, subtree: true, attributes: true,
      attributeFilter: ['data-audit-focus-terminal', 'data-audit-focus-page-terminal',
        'data-audit-focus-active-origin', 'href', 'disabled'] });
    check();
  });
}
