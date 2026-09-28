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

export function isFocusInsideAuditReceipt() {
  return Boolean(document.activeElement?.closest('[data-testid="tenants-audit-receipt"]'));
}

export function isFocusInsideAuditReceiptCorrection() {
  return Boolean(document.activeElement?.closest('.audit-evidence-receipt__correction'));
}

const activeDetailFocus = new Map();

export function restoreDetailFocus(focusId, originTestId, headingId) {
  const requestKey = `${originTestId}:${headingId}`;
  activeDetailFocus.get(requestKey)?.();
  return new Promise(resolve => {
    const start = new URL(window.location.href);
    const originSelector = `[data-testid="${originTestId}"]`;
    let observer;
    let interval;
    let watchdog;
    let finished = false;

    function finish(result) {
      if (finished) return;
      finished = true;
      observer?.disconnect();
      window.clearInterval(interval);
      window.clearTimeout(watchdog);
      window.removeEventListener('pagehide', cancel);
      window.removeEventListener('popstate', check);
      if (activeDetailFocus.get(requestKey) === cancel) activeDetailFocus.delete(requestKey);
      resolve(result);
    }

    function cancel() { finish('cancelled'); }

    function check() {
      if (!equivalentOrigin(start, new URL(window.location.href), null)) { cancel(); return; }
      const origin = document.querySelector(originSelector);
      const link = document.getElementById(focusId);
      if (origin?.contains(link) && link?.matches('a[href]')) {
        link.focus({ preventScroll: false });
        if (document.activeElement === link) { finish('found'); return; }
      }
      if (terminal(origin, 'data-detail-focus-terminal')) {
        focusHeading(headingId, origin);
        finish('missing');
      }
    }

    observer = new MutationObserver(check);
    observer.observe(document, { childList: true, subtree: true, attributes: true,
      attributeFilter: ['data-detail-focus-terminal', 'href'] });
    interval = window.setInterval(check, 250);
    watchdog = window.setTimeout(() => {
      focusHeading(headingId, document.querySelector(originSelector));
      finish('stalled');
    }, 20000);
    activeDetailFocus.set(requestKey, cancel);
    window.addEventListener('pagehide', cancel, { once: true });
    window.addEventListener('popstate', check);
    check();
  });
}

export async function focusDetailLauncher(focusId, originTestId, headingId) {
  return await restoreDetailFocus(focusId, originTestId, headingId) === 'found';
}

const activeAuditFocus = new Map();

function focusHeading(headingId, origin) {
  const heading = document.getElementById(headingId)
    || origin?.querySelector('[data-testid="tenants-detail-state-header"] h1');
  if (heading) {
    if (!heading.hasAttribute('tabindex')) heading.setAttribute('tabindex', '-1');
    heading.focus({ preventScroll: false });
  }
}

function terminal(element, attribute) {
  const value = element?.getAttribute(attribute);
  return value != null && value.toLowerCase() !== 'false';
}

function equivalentOrigin(start, current, expectedUserId) {
  if (start.pathname !== current.pathname || start.hash !== current.hash) return false;
  if (start.searchParams.get('auditFocus') !== current.searchParams.get('auditFocus')) return false;

  const stateKeys = start.pathname === '/tenants'
    ? ['tab', 'scope', 'userId', 'search', 'status', 'sort', 'desc', 'cursor', 'selected', 'anchor']
    : start.pathname === '/tenants/users'
      ? ['tab', 'userId', 'sort', 'cursor', 'selected', 'anchor']
      : start.pathname === '/tenants/my'
        ? ['cursor', 'selected', 'anchor']
        : ['returnUrl', 'auditPartialReturn'];
  const state = key => {
    if (key === 'tab') return start.pathname === '/tenants/users' ? 'users' : 'tenants';
    if (key === 'scope') return 'all';
    if (key === 'desc') return 'false';
    return '';
  };
  for (const key of stateKeys) {
    const before = start.searchParams.get(key) ?? state(key);
    const after = current.searchParams.get(key) ?? state(key);
    if (key === 'userId' && expectedUserId && !before && after === expectedUserId) continue;
    if (before !== after) return false;
  }
  if (expectedUserId && current.searchParams.has('userId')
      && current.searchParams.get('userId') !== expectedUserId) return false;

  const ignored = new Set([...stateKeys, 'auditFocus', 'auditPartialReturn']);
  const other = url => Array.from(url.searchParams.entries())
    .filter(([key]) => !ignored.has(key)).sort(([a], [b]) => a.localeCompare(b));
  return JSON.stringify(other(start)) === JSON.stringify(other(current));
}

export function restoreAuditFocus(focusId, originTestId, headingId, expectedUserId = null) {
  const requestKey = `${originTestId}:${headingId}`;
  activeAuditFocus.get(requestKey)?.();
  return new Promise(resolve => {
    const start = new URL(window.location.href);
    const selector = `[data-testid="${originTestId}"]`;
    const initialPage = Array.from(document.querySelectorAll('[data-audit-focus-heading]'))
      .find(candidate => candidate.getAttribute('data-audit-focus-heading') === headingId);
    const initialActiveOrigin = initialPage?.getAttribute('data-audit-focus-active-origin');
    let observer;
    let routeInterval;
    let watchdog;
    let finished = false;

    function finish(result) {
      if (finished) return;
      finished = true;
      observer?.disconnect();
      window.clearInterval(routeInterval);
      window.clearTimeout(watchdog);
      window.removeEventListener('pagehide', leaveRoute);
      window.removeEventListener('popstate', check);
      window.removeEventListener('hashchange', check);
      if (activeAuditFocus.get(requestKey) === leaveRoute) activeAuditFocus.delete(requestKey);
      resolve(result);
    }

    function leaveRoute() { finish('cancelled'); }

    function check() {
      if (!equivalentOrigin(start, new URL(window.location.href), expectedUserId)) {
        finish('cancelled');
        return;
      }

      const page = Array.from(document.querySelectorAll('[data-audit-focus-heading]'))
        .find(candidate => candidate.getAttribute('data-audit-focus-heading') === headingId);
      if (initialActiveOrigin === originTestId && page
          && page.getAttribute('data-audit-focus-active-origin') !== originTestId) {
        finish('cancelled');
        return;
      }

      const origin = document.querySelector(selector);
      if (!origin) {
        const pageSettled = page && (page.getAttribute('data-audit-focus-active-origin') !== originTestId
          || terminal(page, 'data-audit-focus-page-terminal'));
        if (pageSettled) {
          focusHeading(headingId);
          finish('missing');
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
            finish('found');
            return;
          }
        }
      }

      if (terminal(origin, 'data-audit-focus-terminal')) {
        focusHeading(headingId, origin);
        finish('missing');
      }
    }

    observer = new MutationObserver(check);
    routeInterval = window.setInterval(check, 250);
    watchdog = window.setTimeout(() => {
      focusHeading(headingId, document.querySelector(selector));
      finish('stalled');
    }, 20000);
    activeAuditFocus.set(requestKey, leaveRoute);
    window.addEventListener('pagehide', leaveRoute, { once: true });
    window.addEventListener('popstate', check);
    window.addEventListener('hashchange', check);
    observer.observe(document, { childList: true, subtree: true, attributes: true,
      attributeFilter: ['data-audit-focus-terminal', 'data-audit-focus-page-terminal',
        'data-audit-focus-active-origin', 'href', 'disabled'] });
    check();
  });
}

export async function focusAuditLauncher(focusId, originTestId, headingId, expectedUserId = null) {
  return await restoreAuditFocus(focusId, originTestId, headingId, expectedUserId) === 'found';
}

export function consumeReturnMarkers(expectedFocus = null, expectedUserId = null, detail = false) {
  const url = new URL(window.location.href);
  const marker = detail ? 'anchor' : 'auditFocus';
  if (expectedFocus !== null && url.searchParams.get(marker) !== expectedFocus) return false;
  if (expectedUserId !== null && url.searchParams.get('userId') !== expectedUserId) return false;
  if (!url.searchParams.has(marker) && !url.searchParams.has('auditPartialReturn')
      && !url.searchParams.has('auditReturnUnavailable')) return false;
  url.searchParams.delete(marker);
  url.searchParams.delete('auditPartialReturn');
  url.searchParams.delete('auditReturnUnavailable');
  window.history.replaceState(window.history.state, '', url.pathname + url.search + url.hash);
  return true;
}
