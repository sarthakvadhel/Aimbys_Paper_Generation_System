// PARAKH landing page (Chunk 14) — role tile selection + password toggle.
//
// The view ships with both panels rendered so the page is fully usable
// without JavaScript: a user without JS picks a role via the radio and
// fills in credentials directly. With JS we layer on the React-style
// two-step UX:
//
//   1. Clicking a role tile applies the selected styling, picks the
//      matching radio (so form posts include it), and scrolls to the
//      credentials form.
//   2. The eye/eye-off button toggles password visibility and updates
//      its aria-pressed / aria-label state.
//
// All listeners are attached after DOMContentLoaded; the script is
// safe to include on pages that don't have these elements.

(function () {
    'use strict';

    function initRoleTiles() {
        const tiles = document.querySelectorAll('.aimbys-role-tile');
        if (tiles.length === 0) return;

        const credentialsForm = document.getElementById('aimbys-step-credentials');

        function setSelected(tile) {
            tiles.forEach(function (t) {
                t.classList.remove('aimbys-role-tile--active');
                t.removeAttribute('data-active');
            });
            tile.classList.add('aimbys-role-tile--active');
            tile.dataset.active = 'true';

            const accent = tile.getAttribute('data-accent');
            if (accent) {
                document.documentElement.setAttribute('data-aimbys-accent', accent);
            }

            // Sync the hidden radio so any server-side handler that
            // wants to read the selected role from the form has it.
            const input = tile.querySelector('input[type="radio"]');
            if (input) input.checked = true;
        }

        tiles.forEach(function (tile) {
            tile.addEventListener('click', function (event) {
                // Don't double-fire on label-with-input clicks.
                if (event.target.matches('input[type="radio"]')) return;

                event.preventDefault();
                setSelected(tile);

                if (credentialsForm) {
                    const firstField = credentialsForm.querySelector('input[type="email"], input[type="text"]');
                    if (firstField) {
                        try { firstField.focus({ preventScroll: false }); } catch (e) { firstField.focus(); }
                    }
                }
            });

            // Keyboard activation: pressing Enter on a tile (focusable
            // via its <input>) selects it.
            tile.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    setSelected(tile);
                }
            });
        });

        // Default the second tile (Institute) to selected, mirroring the
        // React reference's `useState<AppRole>('institute')` initial.
        const initial = document.querySelector('.aimbys-role-tile[data-role="institute"]')
                     || tiles[0];
        if (initial) setSelected(initial);
    }

    function initPasswordToggle() {
        const toggle = document.getElementById('aimbys-toggle-password');
        const input = document.getElementById('aimbys-password');
        if (!toggle || !input) return;

        toggle.addEventListener('click', function () {
            const isPassword = input.getAttribute('type') === 'password';
            input.setAttribute('type', isPassword ? 'text' : 'password');
            toggle.setAttribute('aria-pressed', isPassword ? 'true' : 'false');
            toggle.setAttribute('aria-label', isPassword ? 'Hide password' : 'Show password');
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initRoleTiles();
        initPasswordToggle();
    });
})();
