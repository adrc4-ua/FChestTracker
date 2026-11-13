// FCT stats page: sections, sortable tables, the treasure weeks, settings (only when FCT serves the page) and refresh.
(() => {
  const doc = document, body = doc.body;
  const tabs = [...doc.querySelectorAll('[data-tab]')];
  if (tabs.length === 0) return;
  doc.documentElement.classList.add('js'); // without script every section shows, one after another

  // Per browser tab only: the open section, the weeks, the sort order and the scroll survive the page's own reloads.
  const memory = {
    get: key => { try { return sessionStorage.getItem('fct-' + key); } catch { return null; } },
    set: (key, value) => { try { sessionStorage.setItem('fct-' + key, value); } catch { /* storage off */ } },
  };
  const served = location.protocol === 'http:' || location.protocol === 'https:';
  let saving = false;

  // ---- Sections ------------------------------------------------------------------------------------------------
  const open = (id, remember) => {
    const tab = tabs.find(t => t.dataset.tab === id);
    if (!tab) return false;
    for (const t of tabs) {
      const on = t === tab;
      t.setAttribute('aria-selected', String(on));
      t.tabIndex = on ? 0 : -1;
    }
    const strip = tab.parentNode; // in a narrow window the tabs scroll sideways: keep the open one in the middle
    strip.scrollLeft = tab.offsetLeft - (strip.clientWidth - tab.offsetWidth) / 2;
    for (const pane of doc.querySelectorAll('.pane')) pane.hidden = pane.id !== 't-' + id;
    if (remember) memory.set('tab', id);
    return true;
  };
  const go = id => {
    if (!open(id, true)) return;
    window.scrollTo(0, 0);
    try { history.replaceState(null, '', '#' + id); } catch { /* file pages may refuse */ }
  };
  const fromAddress = location.hash.slice(1);
  if (!(fromAddress && open(fromAddress, true)) && !open(memory.get('tab') || '', false)) open(tabs[0].dataset.tab, false);
  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => go(tab.dataset.tab));
    tab.addEventListener('keydown', event => {
      const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[event.key];
      if (!step) return;
      event.preventDefault();
      const next = tabs[(index + step + tabs.length) % tabs.length];
      go(next.dataset.tab);
      next.focus();
    });
  });
  for (const link of doc.querySelectorAll('[data-go]')) {
    link.addEventListener('click', event => { event.preventDefault(); go(link.dataset.go); });
  }

  // ---- Treasure weeks ------------------------------------------------------------------------------------------------
  const weeksButton = doc.querySelector('.weeks-btn'), weeks = doc.getElementById('weeks');
  if (weeksButton && weeks) {
    const showWeeks = visible => {
      weeks.hidden = !visible;
      weeksButton.setAttribute('aria-expanded', String(visible));
      weeksButton.textContent = weeksButton.dataset[visible ? 'close' : 'open'];
      memory.set('weeks', visible ? '1' : '0');
    };
    showWeeks(memory.get('weeks') === '1');
    weeksButton.addEventListener('click', () => showWeeks(weeks.hidden));
  }

  // ---- Sortable tables (teammates) ------------------------------------------------------------------------------------
  for (const table of doc.querySelectorAll('table.sort-table')) {
    const heads = [...table.tHead.rows[0].cells], rows = table.tBodies[0];
    const sortBy = (column, descending) => {
      const numeric = heads[column].dataset.type === 'n';
      const value = row => row.cells[column].dataset.v;
      const order = [...rows.rows].sort((a, b) => {
        const diff = numeric ? Number(value(a)) - Number(value(b)) : value(a).localeCompare(value(b));
        return descending ? -diff : diff;
      });
      order.forEach(row => rows.appendChild(row));
      heads.forEach((head, i) => i === column ? head.setAttribute('aria-sort', descending ? 'descending' : 'ascending') : head.removeAttribute('aria-sort'));
      memory.set('sort', column + (descending ? 'd' : 'a'));
    };
    heads.forEach((head, column) => head.querySelector('button').addEventListener('click', () => {
      const now = head.getAttribute('aria-sort'); // sorted: flip; not yet: numbers high-first, names A-Z
      sortBy(column, now ? now === 'ascending' : head.dataset.type === 'n');
    }));
    const saved = memory.get('sort');
    if (saved && heads[parseInt(saved, 10)]) sortBy(parseInt(saved, 10), saved.endsWith('d'));
  }

  // ---- Settings: changed through FCT's local server --------------------------------------------------------------------
  const toast = doc.querySelector('.toast');
  let toastTimer;
  const say = (message, failed) => {
    if (!toast) return;
    toast.textContent = message;
    toast.className = failed ? 'toast is-bad' : 'toast';
    toast.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => { toast.hidden = true; }, 2600);
  };
  const ping = () => fetch('/api/ping', { cache: 'no-store' });
  const controls = [...doc.querySelectorAll('[data-key]')];
  if (!served) {
    controls.forEach(control => { control.disabled = true; });
    const note = doc.querySelector('.file-note');
    if (note) note.hidden = false;
  }
  const valueOf = control => control.type === 'checkbox' ? (control.checked ? '1' : '0') : control.value;
  for (const control of controls) {
    control.dataset.prev = valueOf(control);
    control.addEventListener('change', () => {
      const key = control.dataset.key, value = valueOf(control);
      saving = true;
      // A new language restarts FCT: wait until it answers again before reloading.
      const reloadWhenBack = triesLeft => {
        if (key !== 'lang' || !served) { saving = false; location.reload(); return; }
        ping().then(reply => { if (!reply.ok) throw new Error(); saving = false; location.reload(); })
          .catch(() => triesLeft > 0 ? setTimeout(() => reloadWhenBack(triesLeft - 1), 250) : (saving = false, location.reload()));
      };
      fetch('/api/ajuste', {
        method: 'POST',
        headers: { 'X-FCT': '1', 'Content-Type': 'application/x-www-form-urlencoded' },
        body: encodeURIComponent(key) + '=' + encodeURIComponent(value),
      })
        .then(reply => reply.json())
        .then(answer => {
          if (!answer.ok) throw new Error();
          say(body.dataset.saved);
          setTimeout(() => reloadWhenBack(16), 1000);
        })
        .catch(() => {
          saving = false;
          say(body.dataset.failed, true);
          if (control.type === 'checkbox') control.checked = control.dataset.prev === '1';
          else control.value = control.dataset.prev;
        });
    });
  }

  // ---- Fresh figures -----------------------------------------------------------------------------------------------------
  // Served by FCT: a quick ping notices a language change (reload at once) or FCT closing (a notice instead of an error
  // page). The page itself reloads every data-refresh seconds, unless the player is busy with a setting.
  const down = doc.querySelector('.down'), pageLanguage = doc.documentElement.lang || '';
  const showDown = () => { if (down) down.hidden = false; };
  let missed = 0;
  if (served) {
    setInterval(() => {
      if (saving) return;
      ping().then(reply => reply.ok ? reply.json() : Promise.reject())
        .then(state => {
          const wasDown = missed >= 3;
          missed = 0;
          if (down) down.hidden = true;
          if ((state.lang && pageLanguage && state.lang !== pageLanguage) || wasDown) location.reload();
        })
        .catch(() => { if (++missed >= 3) showDown(); });
    }, 1500);
  }
  const busy = () => {
    const focused = doc.activeElement;
    return saving || (focused && /^(INPUT|SELECT|TEXTAREA)$/.test(focused.tagName) && focused.closest('.set-sheet'));
  };
  setInterval(() => {
    if (busy()) return;
    if (!served) { location.reload(); return; }
    ping().then(reply => { if (reply.ok) location.reload(); else throw new Error(); }).catch(showDown);
  }, (Number(body.dataset.refresh) || 15) * 1000);

  const scrolled = memory.get('y');
  if (scrolled) window.scrollTo(0, Number(scrolled));
  addEventListener('pagehide', () => memory.set('y', String(window.scrollY)));
})();
