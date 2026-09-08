const test = require('node:test');
const assert = require('node:assert/strict');
const page = require('../Jellyfin.Plugin.Watchlist/Web/page.js');

const noImage = () => null;
const movie = { Id: 'm1', Type: 'Movie', Name: 'Coherence', ProductionYear: 2013 };
const series = { Id: 's1', Type: 'Series', Name: 'Batman', ProductionYear: 1966 };
const episode = { Id: 'e1', Type: 'Episode', Name: 'Pilot', SeriesName: 'The Bold Type', ParentIndexNumber: 1, IndexNumber: 1 };
const season = { Id: 'se1', Type: 'Season', Name: 'Season 2', SeriesName: 'Gilmore Girls' };

test('escapeHtml escapes the five special characters', () => {
  assert.equal(page.escapeHtml(`<a href="x">Tom & Jerry's</a>`), '&lt;a href=&quot;x&quot;&gt;Tom &amp; Jerry&#39;s&lt;/a&gt;');
  assert.equal(page.escapeHtml(null), '');
});

test('groupBySection orders sections, drops empty ones and honours enabled types', () => {
  const sections = page.groupBySection([episode, movie, series], ['Movie', 'Series', 'Season', 'Episode']);
  assert.deepEqual(sections.map((s) => s.title), ['Movies', 'Series', 'Episodes']);
  assert.deepEqual(sections[0].items, [movie]);
  const onlyMovies = page.groupBySection([episode, movie, series], ['Movie']);
  assert.deepEqual(onlyMovies.map((s) => s.type), ['Movie']);
});

test('secondaryText shows year, series name, or series + episode code', () => {
  assert.equal(page.secondaryText(movie), '2013');
  assert.equal(page.secondaryText(season), 'Gilmore Girls');
  assert.equal(page.secondaryText(episode), 'The Bold Type · S1:E1');
  assert.equal(page.secondaryText({ Type: 'Movie', Name: 'x' }), '');
});

test('cardHtml builds a jellyfin-web card with a details link and remove button', () => {
  const html = page.cardHtml({ ...movie, Name: 'A & B' }, () => 'https://img/1');
  assert.match(html, /class="card portraitCard card-hoverable jfw-card"/);
  assert.match(html, /data-id="m1"/);
  assert.match(html, /data-jfw-card="1"/);
  assert.match(html, /href="#\/details\?id=m1"/);
  assert.match(html, /background-image:url\('https:\/\/img\/1'\)/);
  assert.match(html, /class="cardOverlayButton cardOverlayButton-hover paper-icon-button-light jfw-remove"/);
  assert.match(html, /<bdi>A &amp; B<\/bdi>/);
  assert.doesNotMatch(html, /itemAction/);
});

test('cardHtml uses a backdrop card for episodes and no style without an image', () => {
  const html = page.cardHtml(episode, noImage);
  assert.match(html, /class="card backdropCard card-hoverable jfw-card"/);
  assert.match(html, /cardPadder-backdrop/);
  assert.doesNotMatch(html, /style=/);
});

test('sectionHtml renders a heading with the count and one card per item', () => {
  const html = page.sectionHtml({ type: 'Movie', title: 'Movies', items: [movie, series] }, noImage);
  assert.match(html, /<h2 class="sectionTitle sectionTitle-cards padded-left">Movies <span class="jfw-count">2<\/span><\/h2>/);
  assert.equal((html.match(/class="card /g) || []).length, 2);
});

test('pageHtml renders the empty state when there are no sections', () => {
  assert.match(page.pageHtml([], noImage), /jfw-empty/);
  assert.doesNotMatch(page.pageHtml([{ type: 'Movie', title: 'Movies', items: [movie] }], noImage), /jfw-empty/);
});

test('enabledTypesFromConfig maps the config flags to item types', () => {
  assert.deepEqual(page.enabledTypesFromConfig({ ShowMovies: true, ShowSeries: false, ShowSeasons: true, ShowEpisodes: false }), ['Movie', 'Season']);
  assert.deepEqual(page.enabledTypesFromConfig({}), []);
});
