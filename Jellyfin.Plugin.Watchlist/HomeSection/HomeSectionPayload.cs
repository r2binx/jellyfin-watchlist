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
        /// <summary>Other languages fall back to this pack in Home Screen Sections, so one entry labels the row everywhere.</summary>
        public const string TranslationLanguage = "en";

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

        /// <summary>The translation entry Home Screen Sections' settings page uses to label the row's checkbox.</summary>
        public static string TranslationPackJson() => JsonSerializer.Serialize(new Dictionary<string, string> { [SectionId] = DisplayText });
    }
}
