/* ============================================================
   澄明录 — Shared UI Utilities
   ============================================================ */

function setTheme(value) {
  if (value === 'warm') {
    document.documentElement.removeAttribute('data-theme');
  } else {
    document.documentElement.setAttribute('data-theme', value);
  }
  localStorage.setItem('clarity-theme', value);
}

function initThemeSelect() {
  var sel = document.getElementById('theme-select');
  if (!sel) return;
  sel.value = localStorage.getItem('clarity-theme') || 'warm';
}

// 监视 <html> 的 data-theme 属性：Blazor enhanced nav morph 时会抹掉它
(function () {
  var themeObserver = new MutationObserver(function (mutations) {
    var stored = localStorage.getItem('clarity-theme') || 'warm';
    var current = document.documentElement.getAttribute('data-theme') || 'warm';
    if (current !== stored) {
      themeObserver.disconnect();
      if (stored === 'warm') {
        document.documentElement.removeAttribute('data-theme');
      } else {
        document.documentElement.setAttribute('data-theme', stored);
      }
      themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    }
  });
  themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

  // 监视 #theme-select 被重新插入 DOM（Blazor nav 后 body 内容更新）
  new MutationObserver(function (mutations) {
    for (var i = 0; i < mutations.length; i++) {
      var added = mutations[i].addedNodes;
      for (var j = 0; j < added.length; j++) {
        var node = added[j];
        if (node.nodeType !== 1) continue;
        var sel = node.id === 'theme-select' ? node : node.querySelector('#theme-select');
        if (sel) {
          sel.value = localStorage.getItem('clarity-theme') || 'warm';
          return;
        }
      }
    }
  }).observe(document.body, { childList: true, subtree: true });
})();

/**
 * 在指定容器内显示三点脉冲加载动画。
 * @param {HTMLElement} container
 */
function showLoading(container) {
  container.innerHTML =
    '<div class="search-loading">' +
      '<div class="search-loading-dot"></div>' +
      '<div class="search-loading-dot"></div>' +
      '<div class="search-loading-dot"></div>' +
    '</div>';
}
