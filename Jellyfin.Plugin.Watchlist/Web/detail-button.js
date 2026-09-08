/**
 * Jellyfin Watchlist — add/remove button on the item details page.
 */
(function (global) {
  'use strict';

  function itemIdFromHash(hash) {
    const query = (hash || '').split('?')[1];
    if (!query) return null;
    return new URLSearchParams(query).get('id');
  }

  function iconFor(on) { return on ? 'bookmark' : 'bookmark_border'; }
  function titleFor(on) { return on ? 'Remove from watchlist' : 'Add to watchlist'; }

  function buttonHtml(id, on) {
    return `<button is="emby-button" type="button" class="button-flat detailButton emby-button jfw-detail-button" title="${titleFor(on)}" data-id="${id}" data-on="${on ? '1' : '0'}">`
      + `<div class="detailButton-content"><span class="material-icons detailButton-icon ${iconFor(on)}" aria-hidden="true"></span></div></button>`;
  }

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = { itemIdFromHash, buttonHtml };
    return;
  }

  // ---------------------------------------------------------------- browser
  const W = global.JellyfinWatchlist;
  const doc = global.document;
  const LOG = '[Watchlist detail]';

  function paint(btn, on) {
    btn.dataset.on = on ? '1' : '0';
    btn.title = titleFor(on);
    btn.querySelector('.detailButton-icon').className = `material-icons detailButton-icon ${iconFor(on)}`;
  }

  async function onDetailsShow(view) {
    const id = itemIdFromHash(global.location.hash);
    const row = view.querySelector('.mainDetailButtons');
    if (!id || !row) return;
    row.querySelectorAll('.jfw-detail-button').forEach((b) => b.remove());

    let item;
    try {
      item = await W.jf(`/Items/${encodeURIComponent(id)}?userId=${W.userId()}`);
    } catch (err) {
      console.warn(LOG, 'item lookup failed', err);
      return;
    }
    if (!item || !W.isWatchlistType(item.Type)) return;
    if (itemIdFromHash(global.location.hash) !== id) return; // navigated away meanwhile
    if (row.querySelector('.jfw-detail-button')) return;

    const anchor = row.querySelector('.btnUserRating') || row.lastElementChild;
    anchor.insertAdjacentHTML('afterend', buttonHtml(id, !!(item.UserData && item.UserData.Likes)));
    const btn = row.querySelector('.jfw-detail-button');
    btn.addEventListener('click', async () => {
      const on = btn.dataset.on === '1';
      btn.disabled = true;
      try {
        await (on ? W.remove(id) : W.add(id));
      } catch (err) {
        console.warn(LOG, 'toggle failed', err);
        W.toast('Could not update your watchlist');
      } finally {
        btn.disabled = false;
      }
    });
  }

  W.ready.then(() => {
    doc.addEventListener('viewshow', (e) => {
      const view = e.target;
      if (view && view.classList && view.classList.contains('itemDetailPage')) onDetailsShow(view);
    });
    doc.addEventListener('watchlist:changed', () => {
      doc.querySelectorAll('.jfw-detail-button').forEach((btn) => paint(btn, W.isOnWatchlist(btn.dataset.id)));
    });
    // The details view may already be showing when the script loads.
    const current = doc.querySelector('.itemDetailPage:not(.hide)');
    if (current) onDetailsShow(current);
  });
})(typeof window !== 'undefined' ? window : globalThis);
