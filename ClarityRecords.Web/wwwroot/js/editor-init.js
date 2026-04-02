window._editors = window._editors || {};

window.initEasyMDE = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (window._editors[elementId]) return;

    // Fill remaining viewport height: subtract toolbar (~46px), statusbar (0, disabled),
    // admin bottom padding (24px), and a small breathing room (20px)
    const rect = el.getBoundingClientRect();
    const minH = Math.max(300, window.innerHeight - rect.top - 90);

    window._editors[elementId] = new EasyMDE({
        element: el,
        spellChecker: false,
        autosave: { enabled: false },
        status: false,
        minHeight: minH + 'px',
        toolbar: [
            'bold', 'italic', 'heading', '|',
            'quote', 'unordered-list', 'ordered-list', '|',
            'link', 'image', '|',
            'preview', 'side-by-side', 'fullscreen', '|',
            'guide'
        ]
    });
};

window.getEasyMDEContent = function (elementId) {
    return window._editors?.[elementId]?.value() ?? '';
};

window.setEasyMDEContent = function (elementId, content) {
    if (window._editors?.[elementId]) {
        window._editors[elementId].value(content);
    }
};

window.destroyEasyMDE = function (elementId) {
    if (window._editors?.[elementId]) {
        window._editors[elementId].toTextArea();
        delete window._editors[elementId];
    }
};
