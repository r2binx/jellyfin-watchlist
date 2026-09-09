using System.Text.Json;

namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>The registration payload Home Screen Sections expects, as plain values. No route: jellyfin-web
    /// treats unknown route strings as item ids, so a header link cannot point at a Plugin Pages page.</summary>
    public static class HomeSectionPayload
    {
        /// <summary>Home Screen Sections uses this verbatim as a CSS class name, so it must be a valid identifier (a GUID starting with a digit breaks its loader).</summary>
        public const string SectionId = "WatchlistPlugin";
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
