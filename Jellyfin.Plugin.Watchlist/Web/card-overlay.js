/**
 * Jellyfin Watchlist — toggle button in poster card hover overlays.
 * jellyfin-web renders .cardOverlayContainer inline for hoverable cards; CSS shows it on hover.
 */
(function (global) {
  'use strict';

  function iconFor(on) { return on ? 'bookmark' : 'bookmark_border'; }
  function titleFor(on) { return on ? 'Remove from watchlist' : 'Add to watchlist'; }

  function overlayButtonHtml(on) {
    return `<button type="button" is="paper-icon-button-light" class="cardOverlayButton cardOverlayButton-hover paper-icon-button-light jfw-overlay-button" title="${titleFor(on)}" data-on="${on ? '1' : '0'}">`
      + `<span class="material-icons cardOverlayButtonIcon cardOverlayButtonIcon-hover ${iconFor(on)}" aria-hidden="true"></span></button>`;
  }

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = { overlayButtonHtml };
    return;
  }

  // ---------------------------------------------------------------- browser
  const W = global.JellyfinWatchlist;
  const doc = global.document;
  const LOG = '[Watchlist overlay]';

  function paint(btn, on) {
    btn.dataset.on = on ? '1' : '0';
    btn.title = titleFor(on);
    btn.querySelector('.material-icons').className = `material-icons cardOverlayButtonIcon cardOverlayButtonIcon-hover ${iconFor(on)}`;
  }

  function decorate() {
    doc.querySelectorAll('.card[data-id][data-type]:not([data-jfw-card]) .cardOverlayButton-br:not([data-jfw])').forEach((group) => {
      group.dataset.jfw = '1';
      const card = group.closest('.card');
      if (!W.isWatchlistType(card.dataset.type)) return;
      const html = overlayButtonHtml(W.isOnWatchlist(card.dataset.id));
      const menu = group.querySelector('button[data-action="menu"]');
      if (menu) menu.insertAdjacentHTML('beforebegin', html);
      else group.insertAdjacentHTML('beforeend', html);
    });
  }

  W.ready.then(async () => {
    let config;
    try {
      config = await W.getConfig();
    } catch (err) {
      console.warn(LOG, 'config unavailable; overlay buttons disabled', err);
      return;
    }
    if (!config.EnableCardOverlayButtons) return;
    try { await W.load(); } catch (err) { console.warn(LOG, 'initial load failed', err); }

    // Capture phase so jellyfin-web's delegated .itemAction handler never sees the click.
    doc.addEventListener('click', (e) => {
      const btn = e.target.closest && e.target.closest('.jfw-overlay-button');
      if (!btn) return;
      e.preventDefault();
      e.stopPropagation();
      const card = btn.closest('.card');
      const id = card && card.dataset.id;
      if (!id) return;
      const on = btn.dataset.on === '1';
      (on ? W.remove(id) : W.add(id)).catch((err) => {
        console.warn(LOG, 'toggle failed', err);
        W.toast('Could not update your watchlist');
      });
    }, true);

    doc.addEventListener('watchlist:changed', () => {
      doc.querySelectorAll('.jfw-overlay-button').forEach((btn) => {
        const card = btn.closest('.card');
        if (card && card.dataset.id) paint(btn, W.isOnWatchlist(card.dataset.id));
      });
    });

    W.onDomChange(decorate);
  });
})(typeof window !== 'undefined' ? window : globalThis);
