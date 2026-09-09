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

        /// <summary>Clear the watchlist flag once an item is played. Server-wide.</summary>
        public bool AutoRemovePlayed { get; set; } = false;

        /// <summary>Return items for the Home Screen Sections row. Registration happens regardless.</summary>
        public bool EnableHomeSection { get; set; } = true;

        /// <summary>Maximum items in the home row; read through RemovalPolicy.ClampLimit.</summary>
        public int HomeSectionLimit { get; set; } = 16;
    }
}
