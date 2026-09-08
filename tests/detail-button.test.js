const test = require('node:test');
const assert = require('node:assert/strict');
const { itemIdFromHash, buttonHtml } = require('../Jellyfin.Plugin.Watchlist/Web/detail-button.js');

test('itemIdFromHash reads the id query parameter from a details hash', () => {
  assert.equal(itemIdFromHash('#/details?id=abc123&serverId=s'), 'abc123');
  assert.equal(itemIdFromHash('#/home.html'), null);
  assert.equal(itemIdFromHash(''), null);
});

test('buttonHtml renders a detailButton with the right icon and title', () => {
  const on = buttonHtml('x1', true);
  assert.match(on, /class="button-flat detailButton emby-button jfw-detail-button"/);
  assert.match(on, /data-id="x1"/);
  assert.match(on, /title="Remove from watchlist"/);
  assert.match(on, /detailButton-icon bookmark"/);
  const off = buttonHtml('x1', false);
  assert.match(off, /title="Add to watchlist"/);
  assert.match(off, /detailButton-icon bookmark_border"/);
});

test('buttonHtml escapes the id attribute', () => {
  const html = buttonHtml('a"b<c', false);
  assert.match(html, /data-id="a&quot;b&lt;c"/);
  assert.doesNotMatch(html, /data-id="a"b/);
});
