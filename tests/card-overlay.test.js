const test = require('node:test');
const assert = require('node:assert/strict');
const { overlayButtonHtml } = require('../Jellyfin.Plugin.Watchlist/Web/card-overlay.js');

test('overlayButtonHtml renders a card overlay button without itemAction', () => {
  const on = overlayButtonHtml(true);
  assert.match(on, /class="cardOverlayButton cardOverlayButton-hover paper-icon-button-light jfw-overlay-button"/);
  assert.match(on, /cardOverlayButtonIcon-hover bookmark"/);
  assert.match(on, /title="Remove from watchlist"/);
  assert.doesNotMatch(on, /itemAction/);
  assert.match(overlayButtonHtml(false), /bookmark_border"/);
});
