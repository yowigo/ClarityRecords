window._editors = window._editors || {};

window.initEasyMDE = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (window._editors[elementId]) return;
    window._editors[elementId] = new EasyMDE({
        element: el,
        spellChecker: false,
        autosave: { enabled: false },
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
