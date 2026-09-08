# Jellyfin Watchlist

A standalone Jellyfin 12 plugin that adds a **Watchlist** page and add/remove buttons to jellyfin-web.

The watchlist is Jellyfin's own per-user "Likes" flag (`UserData.Likes`). This plugin only adds a UI for it,
so it is compatible with anything else that reads or writes that flag, including Jellyfin-Enhanced's Seerr
watchlist sync.

## Requirements

- Jellyfin 12.x
- [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages) 3.x (provides the sidebar entry)

## Install

1. Dashboard → Plugins → Repositories → add `https://raw.githubusercontent.com/r2binx/jellyfin-watchlist/main/manifest.json`
2. Catalog → install **Watchlist**, restart Jellyfin.
3. Reload the web client. "Watchlist" appears in the sidebar; a bookmark button appears on item pages and poster hover overlays.

## Build

```sh
scripts/build.sh
node --test tests/
```
