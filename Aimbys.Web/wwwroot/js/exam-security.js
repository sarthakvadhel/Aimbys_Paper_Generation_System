/**
 * Exam Security Client — monitors browser behaviour during an exam and
 * reports security events back to the server.
 *
 * Reads configuration from a JSON script block:
 *   <script type="application/json" id="security-profile">{ ... }</script>
 *
 * Expected profile shape:
 * {
 *   attemptId: "guid",
 *   requireFullscreen: true,
 *   detectTabSwitch: true,
 *   detectResize: false,
 *   blockCopyPaste: true,
 *   blockKeyboardShortcuts: true,
 *   heartbeatIntervalSeconds: 30
 * }
 */
(function () {
    'use strict';

    const profileEl = document.getElementById('security-profile');
    if (!profileEl) return;

    let profile;
    try {
        profile = JSON.parse(profileEl.textContent);
    } catch {
        console.warn('[exam-security] Failed to parse security profile.');
        return;
    }

    const attemptId = profile.attemptId;
    if (!attemptId) return;

    const eventQueue = [];
    let flushTimer = null;

    // --- Helpers ---

    function enqueueEvent(eventType, details) {
        eventQueue.push({ attemptId, eventType, detailsJson: details ? JSON.stringify(details) : null });
        scheduleFlush();
    }

    function scheduleFlush() {
        if (flushTimer) return;
        flushTimer = setTimeout(flushEvents, 2000);
    }

    async function flushEvents() {
        flushTimer = null;
        while (eventQueue.length > 0) {
            const evt = eventQueue.shift();
            try {
                await fetch('/Student/Exams/TrackEvent', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(evt)
                });
            } catch {
                // Connection issue — event is lost; heartbeat will detect offline
            }
        }
    }

    // --- Tab visibility ---
    if (profile.detectTabSwitch) {
        document.addEventListener('visibilitychange', function () {
            if (document.hidden) {
                enqueueEvent(3 /* TabBlur */, { timestamp: new Date().toISOString() });
            } else {
                enqueueEvent(4 /* TabFocus */, { timestamp: new Date().toISOString() });
            }
        });
    }

    // --- Fullscreen ---
    if (profile.requireFullscreen) {
        document.addEventListener('fullscreenchange', function () {
            if (!document.fullscreenElement) {
                enqueueEvent(2 /* FullscreenExit */, { timestamp: new Date().toISOString() });
            } else {
                enqueueEvent(1 /* FullscreenEntered */, { timestamp: new Date().toISOString() });
            }
        });
    }

    // --- Resize ---
    if (profile.detectResize) {
        let resizeTimeout;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(function () {
                enqueueEvent(5 /* BrowserResize */, {
                    width: window.innerWidth,
                    height: window.innerHeight
                });
            }, 500);
        });
    }

    // --- Block paste ---
    if (profile.blockCopyPaste) {
        document.addEventListener('paste', function (e) {
            e.preventDefault();
            enqueueEvent(6 /* PasteAttempt */, { timestamp: new Date().toISOString() });
        });
    }

    // --- Block keyboard shortcuts ---
    if (profile.blockKeyboardShortcuts) {
        document.addEventListener('keydown', function (e) {
            // Block Ctrl+C, Ctrl+V, Ctrl+A, F12
            if ((e.ctrlKey || e.metaKey) && ['c', 'v', 'a'].includes(e.key.toLowerCase())) {
                e.preventDefault();
                enqueueEvent(7 /* KeyboardShortcut */, { key: e.key, ctrl: e.ctrlKey });
            }
            if (e.key === 'F12') {
                e.preventDefault();
                enqueueEvent(7 /* KeyboardShortcut */, { key: 'F12' });
            }
        });
    }

    // --- Heartbeat ---
    const heartbeatMs = (profile.heartbeatIntervalSeconds || 30) * 1000;
    setInterval(async function () {
        try {
            await fetch('/Student/Exams/Heartbeat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ attemptId })
            });
        } catch {
            enqueueEvent(8 /* ConnectionLost */, { timestamp: new Date().toISOString() });
        }
    }, heartbeatMs);

    // Initial event
    enqueueEvent(0 /* Started */, { userAgent: navigator.userAgent });

})();
