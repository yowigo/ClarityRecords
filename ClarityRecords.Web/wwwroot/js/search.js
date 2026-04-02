/* ============================================================
   澄明录 — Search Modal (Ctrl+K)
   ============================================================ */

(function () {
  'use strict';

  var modal = null;
  var inputEl = null;
  var resultsEl = null;
  var overlayEl = null;
  var activeIdx = -1;
  var currentItems = [];
  var debounceTimer = null;
  var injected = false;

  function injectModal() {
    if (injected) return;
    injected = true;

    var wrapper = document.createElement('div');
    wrapper.innerHTML =
      '<div id="search-overlay" class="search-overlay"></div>' +
      '<div id="search-modal" class="search-modal" role="dialog" aria-label="搜索" aria-modal="true">' +
        '<div class="search-input-wrap">' +
          '<input id="search-input" type="text" class="search-input"' +
          '       placeholder="搜索文章、标签…" autocomplete="off" spellcheck="false" />' +
        '</div>' +
        '<div id="search-results" class="search-results"></div>' +
        '<div class="search-footer">' +
          '<span>↑↓ 导航</span><span>Enter 打开</span><span>Esc 关闭</span>' +
        '</div>' +
      '</div>';

    document.body.appendChild(wrapper);
    modal     = document.getElementById('search-modal');
    inputEl   = document.getElementById('search-input');
    resultsEl = document.getElementById('search-results');
    overlayEl = document.getElementById('search-overlay');

    inputEl.addEventListener('input', onInput);
    inputEl.addEventListener('keydown', onKeydown);
    overlayEl.addEventListener('click', closeSearch);
  }

  function openSearch() {
    injectModal();
    overlayEl.classList.add('open');
    modal.classList.add('open');
    document.body.classList.add('search-open');
    inputEl.value = '';
    resultsEl.innerHTML = '';
    activeIdx = -1;
    currentItems = [];
    // Slight delay so Blazor navigation doesn't steal focus
    setTimeout(function () { inputEl.focus(); }, 30);
  }

  function closeSearch() {
    if (!modal) return;
    overlayEl.classList.remove('open');
    modal.classList.remove('open');
    document.body.classList.remove('search-open');
  }

  function onInput(e) {
    clearTimeout(debounceTimer);
    var q = e.target.value.trim();
    if (!q) {
      resultsEl.innerHTML = '';
      currentItems = [];
      activeIdx = -1;
      return;
    }
    showLoading(resultsEl);
    debounceTimer = setTimeout(function () { doSearch(q); }, 220);
  }

  function doSearch(q) {
    fetch('/api/search?q=' + encodeURIComponent(q))
      .then(function (res) { return res.ok ? res.json() : []; })
      .then(function (data) {
        currentItems = data;
        activeIdx = -1;
        renderResults(data);
      })
      .catch(function () {
        resultsEl.innerHTML = '<div class="search-no-results">搜索出错，请重试</div>';
      });
  }

  function renderResults(items) {
    if (items.length === 0) {
      resultsEl.innerHTML = '<div class="search-no-results">没有找到相关文章</div>';
      return;
    }
    resultsEl.innerHTML = items.map(function (item, i) {
      var summary = item.summary
        ? '<div class="search-result-summary">' + escapeHtml(item.summary) + '</div>'
        : '';
      return '<a href="/articles/' + item.slug + '" class="search-result-item" data-idx="' + i + '">' +
               '<div class="search-result-title">' + escapeHtml(item.title) + '</div>' +
               summary +
             '</a>';
    }).join('');

    resultsEl.querySelectorAll('.search-result-item').forEach(function (el, i) {
      el.addEventListener('mouseenter', function () { setActive(i); });
      el.addEventListener('click', closeSearch);
    });
  }

  function setActive(idx) {
    activeIdx = idx;
    resultsEl.querySelectorAll('.search-result-item').forEach(function (el, i) {
      el.classList.toggle('active', i === idx);
    });
  }

  function onKeydown(e) {
    if (e.key === 'Escape') {
      e.preventDefault();
      closeSearch();
      return;
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      if (currentItems.length > 0)
        setActive(Math.min(activeIdx + 1, currentItems.length - 1));
      return;
    }
    if (e.key === 'ArrowUp') {
      e.preventDefault();
      if (currentItems.length > 0)
        setActive(Math.max(activeIdx - 1, 0));
      return;
    }
    if (e.key === 'Enter' && activeIdx >= 0 && currentItems[activeIdx]) {
      e.preventDefault();
      closeSearch();
      window.location.href = '/articles/' + currentItems[activeIdx].slug;
    }
  }

  function escapeHtml(str) {
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  // Global Ctrl+K / Cmd+K shortcut
  document.addEventListener('keydown', function (e) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      openSearch();
    }
  });

  // Expose for header button onclick
  window.openSearch = openSearch;
})();

function togglePasswordVisibility() {
  var input = document.getElementById('password');
  var show = document.getElementById('eye-show');
  var hide = document.getElementById('eye-hide');
  if (!input) return;
  if (input.type === 'password') {
    input.type = 'text';
    show.style.display = 'none';
    hide.style.display = 'block';
  } else {
    input.type = 'password';
    show.style.display = 'block';
    hide.style.display = 'none';
  }
}
