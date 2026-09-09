using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Watchlist.Services
{
    /// <summary>Pure decision rules for auto-removal and the home row. No Jellyfin calls, so unit-testable.</summary>
    public static class RemovalPolicy
    {
        private static readonly BaseItemKind[] MovieChain = { BaseItemKind.Movie };
        private static readonly BaseItemKind[] EpisodeChain = { BaseItemKind.Episode, BaseItemKind.Season, BaseItemKind.Series };
        private static readonly BaseItemKind[] SeasonChain = { BaseItemKind.Season, BaseItemKind.Series };
        private static readonly BaseItemKind[] SeriesChain = { BaseItemKind.Series };

        /// <summary>True for a Played transition worth acting on. Rating changes and playback ticks never qualify.</summary>
        public static bool Qualifies(UserDataSaveReason reason, bool? played)
        {
            if (played != true)
            {
                return false;
            }

            return reason is UserDataSaveReason.PlaybackFinished
                or UserDataSaveReason.TogglePlayed
                or UserDataSaveReason.UpdateUserData
                or UserDataSaveReason.Import;
        }

        /// <summary>The item kinds to examine after <paramref name="kind"/> was played: the item, then its parents.</summary>
        public static IReadOnlyList<BaseItemKind> CandidateKinds(BaseItemKind kind) => kind switch
        {
            BaseItemKind.Movie => MovieChain,
            BaseItemKind.Episode => EpisodeChain,
            BaseItemKind.Season => SeasonChain,
            BaseItemKind.Series => SeriesChain,
            _ => Array.Empty<BaseItemKind>(),
        };

        /// <summary>Parents leave the watchlist only once none of their episodes is unplayed.</summary>
        public static bool NeedsEpisodeCheck(BaseItemKind kind) =>
            kind is BaseItemKind.Season or BaseItemKind.Series;

        /// <summary>The Show* flags mapped to item kinds, in the order the page lists them.</summary>
        public static IReadOnlyList<BaseItemKind> EnabledKinds(bool movies, bool series, bool seasons, bool episodes)
        {
            var kinds = new List<BaseItemKind>(4);
            if (movies) kinds.Add(BaseItemKind.Movie);
            if (series) kinds.Add(BaseItemKind.Series);
            if (seasons) kinds.Add(BaseItemKind.Season);
            if (episodes) kinds.Add(BaseItemKind.Episode);
            return kinds;
        }

        public static int ClampLimit(int value) => Math.Clamp(value, 1, 100);
    }
}
