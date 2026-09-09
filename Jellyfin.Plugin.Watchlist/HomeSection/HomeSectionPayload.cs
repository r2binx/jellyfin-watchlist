using System.Text.Json;

namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>The registration payload Home Screen Sections expects, as plain values. No route: jellyfin-web
    /// treats unknown route strings as item ids, so a header link cannot point at a Plugin Pages page.</summary>
    public static class HomeSectionPayload
    {
        public const string SectionId = "7f6a3f7a-6d7c-4b2c-9a1e-3c0c2d8b5e41";
        public const string DisplayText = "Watchlist";
        public const string ResultsClass = "Jellyfin.Plugin.Watchlist.HomeSection.WatchlistSectionResults";
        public const string ResultsMethod = "GetResults";

        public static IReadOnlyDictionary<string, object> Build(string resultsAssembly) => new Dictionary<string, object>
        {
            ["id"] = SectionId,
            ["displayText"] = DisplayText,
            ["limit"] = 1,
            ["resultsAssembly"] = resultsAssembly,
            ["resultsClass"] = ResultsClass,
            ["resultsMethod"] = ResultsMethod,
        };

        public static string ToJson(string resultsAssembly) => JsonSerializer.Serialize(Build(resultsAssembly));
    }
}
