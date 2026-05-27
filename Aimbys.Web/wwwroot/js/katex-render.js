/**
 * KaTeX Auto-Render Script
 * Finds all elements with [data-math] attribute and renders LaTeX content.
 * Also processes $...$ delimiters in question bodies.
 */
(function () {
    'use strict';

    function renderMathElements() {
        var mathElements = document.querySelectorAll('[data-math]');
        mathElements.forEach(function (el) {
            var latex = el.getAttribute('data-math');
            if (latex && window.katex) {
                try {
                    window.katex.render(latex, el, {
                        throwOnError: false,
                        displayMode: el.hasAttribute('data-math-display')
                    });
                } catch (e) {
                    el.textContent = latex;
                    el.classList.add('katex-error');
                }
            }
        });

        // Process inline $...$ delimiters in .question-body elements
        var bodyElements = document.querySelectorAll('.question-body, .rubric-criterion');
        bodyElements.forEach(function (el) {
            if (el.dataset.katexProcessed) return;
            el.dataset.katexProcessed = 'true';

            var html = el.innerHTML;
            // Replace $...$ (non-greedy) with rendered KaTeX spans
            html = html.replace(/\$([^$]+)\$/g, function (match, latex) {
                try {
                    return window.katex.renderToString(latex, { throwOnError: false });
                } catch (e) {
                    return match;
                }
            });
            el.innerHTML = html;
        });
    }

    // Run on DOMContentLoaded and also expose for dynamic content
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', renderMathElements);
    } else {
        renderMathElements();
    }

    window.AimbysKatex = { render: renderMathElements };
})();
