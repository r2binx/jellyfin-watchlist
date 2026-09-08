/**
 * Jellyfin Watchlist — the Watchlist page (rendered into the Plugin Pages view).
 */
(function (global) {
  'use strict';

  const SECTION_ORDER = [['Movie', 'Movies'], ['Series', 'Series'], ['Season', 'Seasons'], ['Episode', 'Episodes']];
  const CONFIG_FLAGS = { Movie: 'ShowMovies', Series: 'ShowSeries', Season: 'ShowSeasons', Episode: 'ShowEpisodes' };

  function escapeHtml(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }

  function enabledTypesFromConfig(config) {
    return Object.keys(CONFIG_FLAGS).filter((type) => !!(config && config[CONFIG_FLAGS[type]]));
  }

  function groupBySection(items, enabledTypes) {
    return SECTION_ORDER
      .filter(([type]) => enabledTypes.includes(type))
      .map(([type, title]) => ({ type, title, items: items.filter((i) => i.Type === type) }))
      .filter((s) => s.items.length > 0);
  }

  function secondaryText(item) {
    if (item.Type === 'Episode') {
      const code = item.ParentIndexNumber != null && item.IndexNumber != null ? `S${item.ParentIndexNumber}:E${item.IndexNumber}` : null;
      return [item.SeriesName, code].filter(Boolean).join(' · ');
    }
    if (item.Type === 'Season') return item.SeriesName || '';
    return item.ProductionYear ? String(item.ProductionYear) : '';
  }

  function cardHtml(item, imageUrl) {
    const wide = item.Type === 'Episode';
    const id = escapeHtml(item.Id);
    const img = imageUrl(item);
    const style = img ? ` style="background-image:url('${escapeHtml(img)}')"` : '';
    return `<div class="card ${wide ? 'backdropCard' : 'portraitCard'} card-hoverable jfw-card" data-id="${id}" data-type="${escapeHtml(item.Type)}" data-jfw-card="1">`
      + '<div class="cardBox cardBox-bottompadded"><div class="cardScalable">'
      + `<div class="cardPadder ${wide ? 'cardPadder-backdrop' : 'cardPadder-portrait'}"></div>`
      + `<a class="cardImageContainer coveredImage cardContent" href="#/details?id=${id}"${style}></a>`
      + '<div class="cardOverlayContainer"><div class="cardOverlayButton-br flex">'
      + `<button type="button" class="cardOverlayButton cardOverlayButton-hover paper-icon-button-light jfw-remove" title="Remove from watchlist" data-id="${id}">`
      + '<span class="material-icons cardOverlayButtonIcon cardOverlayButtonIcon-hover bookmark_remove" aria-hidden="true"></span></button>'
      + '</div></div></div>'
      + `<div class="cardText cardTextCentered cardText-first"><bdi>${escapeHtml(item.Name)}</bdi></div>`
      + `<div class="cardText cardTextCentered cardText-secondary"><bdi>${escapeHtml(secondaryText(item))}</bdi></div>`
      + '</div></div>';
  }

  function sectionHtml(section, imageUrl) {
    return `<div class="verticalSection jfw-section" data-type="${escapeHtml(section.type)}">`
      + `<h2 class="sectionTitle sectionTitle-cards padded-left">${escapeHtml(section.title)} <span class="jfw-count">${section.items.length}</span></h2>`
      + `<div class="itemsContainer vertical-wrap padded-left padded-right">${section.items.map((i) => cardHtml(i, imageUrl)).join('')}</div>`
      + '</div>';
  }

  function pageHtml(sections, imageUrl) {
    if (!sections.length) {
      return '<div class="jfw-empty"><span class="material-icons jfw-empty-icon" aria-hidden="true">bookmark_border</span>'
        + '<h2>Your watchlist is empty</h2><p>Add movies and shows from their details page, or let Seerr sync them here.</p></div>';
    }
    return sections.map((s) => sectionHtml(s, imageUrl)).join('');
  }

  if (typeof module !== 'undefined' && module.exports) {
    module.exports = { escapeHtml, enabledTypesFromConfig, groupBySection, secondaryText, cardHtml, sectionHtml, pageHtml };
    return;
  }

  // ---------------------------------------------------------------- browser
  const W = global.JellyfinWatchlist;
  const doc = global.document;
  const LOG = '[Watchlist page]';

  function imageUrl(item) {
    const tags = item.ImageTags || {};
    if (tags.Primary) return global.ApiClient.getImageUrl(item.Id, { type: 'Primary', maxWidth: 400, tag: tags.Primary });
    if (item.SeriesId && item.SeriesPrimaryImageTag) return global.ApiClient.getImageUrl(item.SeriesId, { type: 'Primary', maxWidth: 400, tag: item.SeriesPrimaryImageTag });
    return null;
  }

  const inflight = new WeakMap();

  async function renderNow(root) {
    root.innerHTML = '<div class="jfw-loading">Loading…</div>';
    try {
      const types = enabledTypesFromConfig(await W.getConfig());
      let items = [];
      if (types.length) {
        const res = await W.jf(`/Items?userId=${W.userId()}&Filters=Likes&Recursive=true&IncludeItemTypes=${types.join(',')}`
          + '&Fields=ProductionYear,SeriesName,SeriesPrimaryImage,ParentIndexNumber,IndexNumber&SortBy=SortName&SortOrder=Ascending&Limit=10000');
        items = res && res.Items ? res.Items : [];
      }
      root.innerHTML = pageHtml(groupBySection(items, types), imageUrl);
    } catch (err) {
      console.warn(LOG, 'load failed', err);
      root.innerHTML = '<div class="jfw-error"><p>Could not load your watchlist.</p>'
        + '<button type="button" is="emby-button" class="raised jfw-retry">Retry</button></div>';
    }
  }

  function render(root) {
    if (inflight.has(root)) {
      inflight.set(root, true); // re-render once the current pass finishes
      return;
    }
    inflight.set(root, false);
    renderNow(root).finally(() => {
      const again = inflight.get(root);
      inflight.delete(root);
      if (again) render(root);
    });
  }

  function updateCount(section) {
    const count = section.querySelectorAll('.jfw-card').length;
    if (count === 0) { section.remove(); return; }
    section.querySelector('.jfw-count').textContent = String(count);
  }

  function dropCard(root, id) {
    root.querySelectorAll(`.jfw-card[data-id="${CSS.escape(id)}"]`).forEach((card) => {
      const section = card.closest('.jfw-section');
      card.remove();
      if (section) updateCount(section);
    });
    if (!root.querySelector('.jfw-card')) root.innerHTML = pageHtml([], imageUrl);
  }

  function wire(root) {
    root.addEventListener('click', async (e) => {
      const retry = e.target.closest('.jfw-retry');
      if (retry) { render(root); return; }
      const btn = e.target.closest('.jfw-remove');
      if (!btn) return;
      e.preventDefault();
      e.stopPropagation();
      const id = btn.dataset.id;
      const card = btn.closest('.jfw-card');
      const section = card.closest('.jfw-section');
      const placeholder = doc.createComment('jfw');
      card.replaceWith(placeholder);
      updateCount(section);
      try {
        await W.remove(id);
        if (!root.querySelector('.jfw-card')) root.innerHTML = pageHtml([], imageUrl);
      } catch (err) {
        console.warn(LOG, 'remove failed', err);
        if (placeholder.parentNode) placeholder.replaceWith(card); else root.prepend(card);
        if (section.isConnected) updateCount(section); else render(root);
        W.toast('Could not update your watchlist');
      }
    });
  }

  W.ready.then(() => {
    W.onDomChange(() => {
      const root = doc.querySelector('.userPreferencesPage:not(.hide) .jfw-root:not([data-jfw-mounted])');
      if (!root) return;
      root.dataset.jfwMounted = '1';
      wire(root);
      render(root);
    });

    doc.addEventListener('viewshow', (e) => {
      const root = e.target && e.target.querySelector ? e.target.querySelector('.jfw-root[data-jfw-mounted]') : null;
      if (root) render(root);
    });

    doc.addEventListener('watchlist:changed', (e) => {
      const root = doc.querySelector('.jfw-root[data-jfw-mounted]');
      if (!root || !root.offsetParent) return;
      const ids = e.detail && e.detail.ids;
      if (!Array.isArray(ids)) return;
      ids.forEach((id) => { if (!W.isOnWatchlist(id)) dropCard(root, id); });
    });
  });
})(typeof window !== 'undefined' ? window : globalThis);
