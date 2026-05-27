/**
 * TinyMCE Insert Equation Plugin
 * Adds an "Insert Equation" button that opens a LaTeX input modal.
 * Saves as $...$ delimiters which katex-render.js processes.
 */
(function () {
    'use strict';

    tinymce.PluginManager.add('equation', function (editor) {
        editor.ui.registry.addButton('equation', {
            text: '\u03A3 Equation',
            tooltip: 'Insert LaTeX Equation',
            onAction: function () {
                editor.windowManager.open({
                    title: 'Insert Equation',
                    body: {
                        type: 'panel',
                        items: [
                            {
                                type: 'textarea',
                                name: 'latex',
                                label: 'LaTeX Expression',
                                placeholder: 'e.g. \\frac{a}{b} or x^2 + y^2 = z^2'
                            }
                        ]
                    },
                    buttons: [
                        { type: 'cancel', text: 'Cancel' },
                        { type: 'submit', text: 'Insert', primary: true }
                    ],
                    onSubmit: function (api) {
                        var data = api.getData();
                        if (data.latex && data.latex.trim()) {
                            editor.insertContent(
                                '<span data-math="' +
                                data.latex.trim().replace(/"/g, '&quot;') +
                                '">$' + data.latex.trim() + '$</span>'
                            );
                        }
                        api.close();
                    }
                });
            }
        });
    });
})();
