using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Watchlist
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool ShowMovies { get; set; } = true;
        public bool ShowSeries { get; set; } = true;
        public bool ShowSeasons { get; set; } = true;
        public bool ShowEpisodes { get; set; } = true;
        public bool EnableCardOverlayButtons { get; set; } = true;
    }
}
