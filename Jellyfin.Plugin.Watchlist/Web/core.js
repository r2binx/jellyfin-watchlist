/**
 * Jellyfin Watchlist — core: shared state, Jellyfin API access, DOM helpers.
 * The watchlist is Jellyfin's per-user UserData.Likes flag. This file owns the
 * in-memory id set and exposes window.JellyfinWatchlist for the other modules.
 */
(function (global) {
  'use strict';

  const WATCHLIST_TYPES = ['Movie', 'Series', 'Season', 'Episode'];

  /**
   * Pure state factory (no DOM, no network).
   * @param {{fetchIds: () => Promise<string[]>, setLike: (id: string, liked: boolean) => Promise<any>}} api
   * @param {(detail: {ids: string[] | null}) => void} emit
   */
  function createWatchlistState(api, emit) {
    const ids = new Set();
    let loading = null;
    let generation = 0;

    function load() {
      if (!loading) {
        const gen = ++generation;
        loading = api.fetchIds().then((list) => {
          if (gen !== generation) return; // superseded by a newer refresh
          ids.clear();
          list.forEach((id) => ids.add(id));
          emit({ ids: null });
        }).catch((err) => {
          if (gen === generation) loading = null;
          throw err;
        });
      }
      return loading;
    }

    function refresh() {
      loading = null;
      return load();
    }

    async function set(id, liked) {
      const before = ids.has(id);
      if (liked) ids.add(id); else ids.delete(id);
      emit({ ids: [id] });
      try {
        await api.setLike(id, liked);
      } catch (err) {
        if (before) ids.add(id); else ids.delete(id);
        emit({ ids: [id] });
        throw err;
      }
    }

    return {
      load,
      refresh,
      has: (id) => ids.has(id),
      add: (id) => set(id, true),
      remove: (id) => set(id, false),
      toggle: (id) => set(id, !ids.has(id)),
      size: () => ids.size,
    };
  }

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = { createWatchlistState, WATCHLIST_TYPES };
    return;
  }

  // ---------------------------------------------------------------- browser
  const LOG = '[Watchlist]';
  const doc = global.document;

  function userId() {
    return global.ApiClient.getCurrentUserId();
  }

  /** GET/POST/DELETE a Jellyfin server path with the web client's credentials; resolves parsed JSON. */
  function jf(path, options) {
    const opts = Object.assign({ type: 'GET', dataType: 'json' }, options || {});
    opts.url = global.ApiClient.getUrl(path);
    return global.ApiClient.ajax(opts);
  }

  const api = {
    fetchIds: () => jf(`/Items?userId=${userId()}&Filters=Likes&Recursive=true&IncludeItemTypes=${WATCHLIST_TYPES.join(',')}&Limit=10000`)
      .then((res) => (res && res.Items ? res.Items : []).map((i) => i.Id)),
    setLike: (id, liked) => jf(`/UserItems/${encodeURIComponent(id)}/Rating?userId=${userId()}${liked ? '&likes=true' : ''}`, { type: liked ? 'POST' : 'DELETE' }),
  };

  const state = createWatchlistState(api, (detail) => {
    doc.dispatchEvent(new CustomEvent('watchlist:changed', { detail }));
  });

  // Shared DOM observer: one MutationObserver, callbacks coalesced per animation frame.
  const domCallbacks = [];
  let frame = false;
  function runDomCallbacks() {
    frame = false;
    domCallbacks.forEach((cb) => { try { cb(); } catch (err) { console.warn(LOG, 'dom callback failed', err); } });
  }
  function scheduleDomCallbacks() {
    if (!frame) { frame = true; global.requestAnimationFrame(runDomCallbacks); }
  }
  function onDomChange(cb) {
    domCallbacks.push(cb);
    scheduleDomCallbacks();
  }

  let configPromise = null;
  function getConfig() {
    if (!configPromise) {
      configPromise = jf('/Watchlist/config').catch((err) => {
        configPromise = null;
        throw err;
      });
    }
    return configPromise;
  }

  function toast(text) {
    const el = doc.createElement('div');
    el.className = 'jfw-toast';
    el.textContent = text;
    doc.body.appendChild(el);
    global.requestAnimationFrame(() => el.classList.add('jfw-toast-visible'));
    global.setTimeout(() => {
      el.classList.remove('jfw-toast-visible');
      global.setTimeout(() => el.remove(), 300);
    }, 3000);
  }

  function waitForApiClient() {
    return new Promise((resolve) => {
      (function check() {
        const a = global.ApiClient;
        if (a && typeof a.getCurrentUserId === 'function' && a.getCurrentUserId()) resolve();
        else global.setTimeout(check, 250);
      })();
    });
  }

  const W = {
    version: '1',
    types: WATCHLIST_TYPES,
    isWatchlistType: (type) => WATCHLIST_TYPES.includes(type),
    ready: null,
    load: () => state.load(),
    refresh: () => state.refresh(),
    isOnWatchlist: (id) => state.has(id),
    add: (id) => state.add(id),
    remove: (id) => state.remove(id),
    toggle: (id) => state.toggle(id),
    getConfig,
    onDomChange,
    jf,
    userId,
    toast,
  };
  global.JellyfinWatchlist = W;

  W.ready = waitForApiClient().then(() => {
    new MutationObserver(scheduleDomCallbacks).observe(doc.body, { childList: true, subtree: true });

    // Keep the id set fresh across navigation and user switches (throttled).
    let lastRefresh = 0;
    let lastUser = userId();
    doc.addEventListener('viewshow', () => {
      const now = Date.now();
      const user = userId();
      if (!user) return;
      if (user !== lastUser || now - lastRefresh > 2000) {
        lastUser = user;
        lastRefresh = now;
        state.refresh().catch((err) => console.warn(LOG, 'refresh failed', err));
      }
    });
    console.log(LOG, 'ready');
  });
})(typeof window !== 'undefined' ? window : globalThis);
