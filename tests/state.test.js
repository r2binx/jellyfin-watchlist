const test = require('node:test');
const assert = require('node:assert/strict');
const { createWatchlistState, WATCHLIST_TYPES } = require('../Jellyfin.Plugin.Watchlist/Web/core.js');

function makeApi(overrides = {}) {
  const calls = [];
  return {
    calls,
    fetchIds: overrides.fetchIds || (async () => ['a', 'b']),
    setLike: overrides.setLike || (async (id, liked) => { calls.push([id, liked]); }),
  };
}

test('exports the four watchlist item types', () => {
  assert.deepEqual(WATCHLIST_TYPES, ['Movie', 'Series', 'Season', 'Episode']);
});

test('load populates the id set and emits a full change', async () => {
  const api = makeApi();
  const events = [];
  const state = createWatchlistState(api, (d) => events.push(d));
  await state.load();
  assert.equal(state.has('a'), true);
  assert.equal(state.has('z'), false);
  assert.equal(state.size(), 2);
  assert.deepEqual(events, [{ ids: null }]);
});

test('load is shared while in flight and cached afterwards', async () => {
  let n = 0;
  const api = makeApi({ fetchIds: async () => { n++; return ['a']; } });
  const state = createWatchlistState(api, () => {});
  await Promise.all([state.load(), state.load()]);
  await state.load();
  assert.equal(n, 1);
  await state.refresh();
  assert.equal(n, 2);
});

test('add is optimistic, calls the api and emits the id', async () => {
  const api = makeApi();
  const events = [];
  const state = createWatchlistState(api, (d) => events.push(d));
  await state.load();
  const p = state.add('c');
  assert.equal(state.has('c'), true); // before the api resolves
  await p;
  assert.deepEqual(api.calls, [['c', true]]);
  assert.deepEqual(events.at(-1), { ids: ['c'] });
});

test('remove failure reverts the set and rethrows', async () => {
  const api = makeApi({ setLike: async () => { throw new Error('boom'); } });
  const state = createWatchlistState(api, () => {});
  await state.load();
  await assert.rejects(state.remove('a'), /boom/);
  assert.equal(state.has('a'), true);
});

test('toggle removes present ids and adds missing ones', async () => {
  const api = makeApi();
  const state = createWatchlistState(api, () => {});
  await state.load();
  await state.toggle('a');
  await state.toggle('q');
  assert.deepEqual(api.calls, [['a', false], ['q', true]]);
  assert.equal(state.has('a'), false);
  assert.equal(state.has('q'), true);
});

test('a failed load can be retried', async () => {
  let first = true;
  const api = makeApi({ fetchIds: async () => { if (first) { first = false; throw new Error('net'); } return ['x']; } });
  const state = createWatchlistState(api, () => {});
  await assert.rejects(state.load(), /net/);
  await state.load();
  assert.equal(state.has('x'), true);
});

test('a stale in-flight load does not overwrite a newer refresh', async () => {
  let resolveFirst;
  let calls = 0;
  const api = makeApi({
    fetchIds: () => {
      calls++;
      if (calls === 1) return new Promise((resolve) => { resolveFirst = resolve; });
      return Promise.resolve(['new']);
    },
  });
  const events = [];
  const state = createWatchlistState(api, (d) => events.push(d));
  const first = state.load();
  await state.refresh();
  assert.equal(state.has('new'), true);
  resolveFirst(['stale']);
  await first;
  assert.equal(state.has('new'), true);
  assert.equal(state.has('stale'), false);
  assert.equal(events.length, 1);
});
