// Chart.js binding helper for ChartCardViewComponent (Chunk 16).
//
// The view component renders <canvas data-aimbys-chart="line|bar|pie|..."
// data-aimbys-chart-url="/path/to/data.json"> and this script:
//   1) finds every such canvas after DOMContentLoaded
//   2) fetches its JSON URL
//   3) renders a Chart.js chart with reasonable defaults
//
// JSON shape: { labels: [...], datasets: [{ label, data, ... }] }
//   - The datasets array passes through to Chart.js as-is so server
//     code can pick colours / borders / fills declaratively.
//
// Chart.js itself is loaded from a CDN in _RoleLayout.cshtml with an
// SRI hash; see that file for the version pin. If Chart.js fails to
// load (offline / blocked), the canvas is replaced with a small
// caption so the dashboard never renders a silent blank box.

(function () {
    'use strict';

    function fallbackText(canvas, message) {
        var note = document.createElement('div');
        note.className = 'text-secondary small text-center py-3';
        note.textContent = message;
        if (canvas.parentNode) {
            canvas.parentNode.replaceChild(note, canvas);
        }
    }

    /**
     * Resolves the brand accent for the current role shell. Falls
     * back to the platform blue when the page isn't inside a role
     * shell (e.g. the demo card on the privacy page).
     */
    function getRoleAccent() {
        var styles = window.getComputedStyle(document.documentElement);
        var accent = styles.getPropertyValue('--aimbys-role-accent').trim();
        return accent || '#1d4ed8';
    }

    function applyDefaultColours(chartType, datasets, accent) {
        if (!Array.isArray(datasets)) return datasets;

        // Pick a small palette derived from the role accent so every
        // chart on a dashboard reads as part of the same surface.
        var palette = [
            accent,
            '#7c3aed',
            '#0369a1',
            '#15803d',
            '#d97706',
            '#dc2626'
        ];

        return datasets.map(function (ds, idx) {
            var clone = Object.assign({}, ds);
            var c = palette[idx % palette.length];

            if (chartType === 'line' || chartType === 'area') {
                if (clone.borderColor === undefined) clone.borderColor = c;
                if (clone.backgroundColor === undefined) {
                    clone.backgroundColor = c + '33'; // ~20% opacity
                }
                if (clone.tension === undefined) clone.tension = 0.3;
                if (chartType === 'area' && clone.fill === undefined) clone.fill = true;
            } else if (chartType === 'bar') {
                if (clone.backgroundColor === undefined) clone.backgroundColor = c;
                if (clone.borderRadius === undefined) clone.borderRadius = 4;
            } else if (chartType === 'pie' || chartType === 'doughnut') {
                if (clone.backgroundColor === undefined) {
                    clone.backgroundColor = palette.slice(0, (clone.data || []).length);
                }
            }

            return clone;
        });
    }

    function bindCanvas(canvas) {
        if (typeof window.Chart === 'undefined') {
            fallbackText(canvas, 'Charts unavailable: Chart.js failed to load.');
            return;
        }

        var chartType = canvas.getAttribute('data-aimbys-chart') || 'line';
        var url = canvas.getAttribute('data-aimbys-chart-url');
        if (!url) {
            fallbackText(canvas, 'No chart data URL configured.');
            return;
        }

        // 'area' is rendered as a filled line chart in Chart.js 4.
        var chartJsType = chartType === 'area' ? 'line' : chartType;

        fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
            .then(function (response) {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then(function (payload) {
                var accent = getRoleAccent();
                var datasets = applyDefaultColours(chartType, payload.datasets, accent);

                /* eslint-disable no-new */
                new window.Chart(canvas, {
                    type: chartJsType,
                    data: {
                        labels: payload.labels || [],
                        datasets: datasets
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } },
                            tooltip: { intersect: false, mode: 'index' }
                        },
                        scales: (chartType === 'pie' || chartType === 'doughnut') ? undefined : {
                            x: { grid: { display: false }, ticks: { font: { size: 11 } } },
                            y: { grid: { color: 'rgba(148, 163, 184, 0.15)' }, ticks: { font: { size: 11 } } }
                        }
                    }
                });
            })
            .catch(function (err) {
                if (window.console) {
                    window.console.warn('[aimbys-chart] failed', url, err);
                }
                fallbackText(canvas, 'Chart data unavailable.');
            });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var canvases = document.querySelectorAll('canvas[data-aimbys-chart]');
        canvases.forEach(bindCanvas);
    });
})();
