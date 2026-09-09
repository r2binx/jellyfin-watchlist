# Jellyfin Watchlist

A standalone Jellyfin 12 plugin that adds a **Watchlist** page, add/remove buttons, an optional home screen row,
and optional auto-removal of played items to jellyfin-web.

The watchlist is Jellyfin's own per-user "Likes" flag (`UserData.Likes`). This plugin only adds a UI and a bit
of automation around it, so it is compatible with anything else that reads or writes that flag, including
Jellyfin-Enhanced's Seerr watchlist sync.

## Requirements

- Jellyfin 12.x
- Optional: [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages) 3.x for the sidebar Watchlist page. The details-page and overlay buttons work without it.
- Optional: [Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) 3.x for the Watchlist home screen row. After installing Watchlist, open Dashboard → Plugins → Home Screen Sections → Section Settings once and save: that plugin only lists sections that have an entry there, and its settings page adds newly registered ones. Users then turn the row on under Settings → Modular Home like any other section.

## Install

1. Dashboard → Plugins → Repositories → add `https://raw.githubusercontent.com/r2binx/jellyfin-watchlist/main/manifest.json`
2. Catalog → install **Watchlist**, restart Jellyfin.
3. Reload the web client. "Watchlist" appears in the sidebar; a bookmark button appears on item pages and poster hover overlays.

## Configuration

Dashboard → Plugins → Watchlist:

- **Item types** (Movies, Series, Seasons, Episodes): what the Watchlist page and the home screen row list.
- **Overlay button**: whether the poster hover overlay gets a watchlist button.
- **Remove items from the watchlist once played** (default off): movies and episodes leave when played, series and
  seasons leave once none of their episodes is unplayed, specials included. Marking played by hand counts. Applies to every user.
  Seerr is not touched; Jellyfin-Enhanced's "prevent re-addition" option keeps its sync from adding the item back.
- **Provide a Watchlist row for Home Screen Sections** (default on) and **Maximum items in the home row**
  (default 16): the row shows the newest library additions on the watchlist first.

## Build

```sh
scripts/build.sh          # plugin DLL
scripts/build.sh test     # C# tests
node --test 'tests/**/*.test.js'
```

## License

MIT. See [LICENSE](LICENSE).
