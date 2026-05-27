// Role-area shell (Chunk 15) — dark-mode toggle persistence.
//
// The four role layouts (SuperAdmin / Institute / Teacher / Student)
// share a topbar that includes a sun/moon button. Clicking the button:
//   1) flips data-bs-theme on <html> between 'light' and 'dark'
//   2) writes a 1-year cookie ('aimbys-theme') so the choice persists
//      across reloads
//   3) updates aria-pressed for screen readers
//
// On page load, we re-apply the cookie so the dark theme survives
// hard refreshes. The cookie is the single source of truth — server-
// side preference plumbing lands with the user-preferences UI in a
// later chunk; until then this is the simplest path that meets
// "dark-mode toggle (cookie)" from the Chunk 15 spec.

(function () {
    'use strict';

    const THEME_COOKIE = 'aimbys-theme';
    const ONE_YEAR_SECONDS = 60 * 60 * 24 * 365;

    function readCookie(name) {
        const match = document.cookie.match(
            new RegExp('(?:^|;\\s*)' + name + '=([^;]*)'));
        return match ? decodeURIComponent(match[1]) : null;
    }

    function writeCookie(name, value) {
        document.cookie =
            name + '=' + encodeURIComponent(value)
            + '; Path=/'
            + '; Max-Age=' + ONE_YEAR_SECONDS
            + '; SameSite=Lax';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);

        const toggle = document.getElementById('aimbys-darkmode-toggle');
        if (toggle) {
            const isDark = theme === 'dark';
            toggle.setAttribute('aria-pressed', isDark ? 'true' : 'false');
            toggle.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        const stored = readCookie(THEME_COOKIE);
        if (stored === 'dark' || stored === 'light') {
            applyTheme(stored);
        }

        const toggle = document.getElementById('aimbys-darkmode-toggle');
        if (!toggle) return;

        toggle.addEventListener('click', function () {
            const current = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            applyTheme(next);
            writeCookie(THEME_COOKIE, next);
        });
    });
})();
