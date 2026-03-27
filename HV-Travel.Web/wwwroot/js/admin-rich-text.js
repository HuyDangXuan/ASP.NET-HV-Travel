(function () {
    'use strict';

    var SELECTOR = 'textarea[data-rich-editor="true"]';
    var editorIdCounter = 0;
    var submitHookBound = false;

    function ensureEditorId(textarea) {
        if (!textarea.id) {
            editorIdCounter += 1;
            textarea.id = 'admin-rich-editor-' + editorIdCounter;
        }

        return textarea.id;
    }

    function isTinyReady() {
        return typeof window.tinymce !== 'undefined' && typeof window.tinymce.init === 'function';
    }

    function syncTextarea(textarea) {
        if (!textarea) return;
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
        textarea.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function buildOptions(textarea) {
        var isDark = document.documentElement.classList.contains('dark');
        var height = parseInt(textarea.getAttribute('data-editor-height') || '', 10);
        var bodyBackground = isDark ? '#0f172a' : '#ffffff';
        var bodyForeground = isDark ? '#e2e8f0' : '#0f172a';
        var mutedForeground = isDark ? '#94a3b8' : '#475569';
        var borderColor = isDark ? '#334155' : '#cbd5e1';

        return {
            target: textarea,
            menubar: false,
            branding: false,
            promotion: false,
            license_key: 'gpl',
            plugins: 'autolink link lists table code preview searchreplace visualblocks wordcount autoresize',
            toolbar: 'undo redo | blocks | bold italic underline strikethrough | bullist numlist | alignleft aligncenter alignright | blockquote table link | removeformat code preview',
            block_formats: '\u0110o\u1ea1n v\u0103n=p; Ti\u00eau \u0111\u1ec1 2=h2; Ti\u00eau \u0111\u1ec1 3=h3; Ti\u00eau \u0111\u1ec1 4=h4',
            min_height: Number.isFinite(height) ? height : 220,
            autoresize_bottom_margin: 16,
            resize: false,
            statusbar: true,
            convert_urls: false,
            browser_spellcheck: true,
            contextmenu: false,
            skin: isDark ? 'oxide-dark' : 'oxide',
            content_css: isDark ? 'dark' : 'default',
            content_style: [
                'html { background: ' + bodyBackground + '; }',
                'body { font-family: "Plus Jakarta Sans", sans-serif; font-size: 15px; line-height: 1.7; background: ' + bodyBackground + '; color: ' + bodyForeground + '; padding: 0.75rem 0.9rem; }',
                'p { margin: 0 0 0.85rem; }',
                'h2,h3,h4 { margin: 1.2rem 0 0.75rem; font-weight: 800; }',
                'ul,ol { padding-left: 1.25rem; }',
                'blockquote { border-left: 3px solid #38bdf8; margin: 1rem 0; padding-left: 1rem; color: ' + mutedForeground + '; }',
                'a { color: #38bdf8; }',
                'table { border-collapse: collapse; width: 100%; }',
                'table td, table th { border: 1px solid ' + borderColor + '; padding: 0.55rem 0.7rem; }'
            ].join(' '),
            setup: function (editor) {
                editor.on('init', function () {
                    editor.getBody().style.backgroundColor = bodyBackground;
                    editor.getBody().style.color = bodyForeground;
                    editor.save();
                    syncTextarea(textarea);
                });

                editor.on('change input undo redo keyup setcontent', function () {
                    editor.save();
                    syncTextarea(textarea);
                });
            }
        };
    }

    function initTextarea(textarea) {
        if (!isTinyReady() || !textarea || textarea.dataset.richEditorInitialized === 'true') return;

        ensureEditorId(textarea);
        window.tinymce.init(buildOptions(textarea));
        textarea.dataset.richEditorInitialized = 'true';
    }

    function initWithin(root) {
        if (!isTinyReady()) return;

        var scope = root && root.querySelectorAll ? root : document;
        var textareas = scope.querySelectorAll(SELECTOR);
        textareas.forEach(initTextarea);
    }

    function removeWithin(root) {
        if (!isTinyReady()) return;

        var scope = root && root.querySelectorAll ? root : document;
        var textareas = scope.matches && scope.matches(SELECTOR)
            ? [scope]
            : Array.prototype.slice.call(scope.querySelectorAll(SELECTOR));

        textareas.forEach(function (textarea) {
            if (!textarea.id) return;
            var editor = window.tinymce.get(textarea.id);
            if (editor) {
                editor.save();
                editor.remove();
            }
            delete textarea.dataset.richEditorInitialized;
        });
    }

    function reinitializeAll() {
        if (!isTinyReady()) return;

        removeWithin(document);
        initWithin(document);
    }

    function bindSubmitHook() {
        if (submitHookBound) return;
        submitHookBound = true;

        document.addEventListener('submit', function () {
            if (isTinyReady()) {
                window.tinymce.triggerSave();
            }
        }, true);
    }

    window.AdminRichText = {
        init: initWithin,
        refresh: initWithin,
        remove: removeWithin,
        reinitializeAll: reinitializeAll
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            bindSubmitHook();
            initWithin(document);
        }, { once: true });
    } else {
        bindSubmitHook();
        initWithin(document);
    }
})();
