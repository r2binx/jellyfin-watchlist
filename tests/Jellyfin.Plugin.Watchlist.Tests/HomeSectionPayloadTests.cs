using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Watchlist.HomeSection;
using Xunit;

namespace Jellyfin.Plugin.Watchlist.Tests;

public class HomeSectionPayloadTests
{
    [Fact]
    public void Build_ContainsExactlyTheRegistrationFields()
    {
        var payload = HomeSectionPayload.Build("Jellyfin.Plugin.Watchlist, Version=1.1.0.0");

        Assert.Equal(
            new[] { "displayText", "id", "limit", "resultsAssembly", "resultsClass", "resultsMethod" },
            payload.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("WatchlistPlugin", payload["id"]);
        Assert.Equal("Watchlist", payload["displayText"]);
        Assert.Equal(1, payload["limit"]);
        Assert.Equal("Jellyfin.Plugin.Watchlist, Version=1.1.0.0", payload["resultsAssembly"]);
        Assert.Equal("Jellyfin.Plugin.Watchlist.HomeSection.WatchlistSectionResults", payload["resultsClass"]);
        Assert.Equal("GetResults", payload["resultsMethod"]);
    }

    [Fact]
    public void SectionId_IsAValidCssClassNameBecauseTheLoaderUsesItAsOne()
    {
        Assert.Matches(new Regex("^[A-Za-z_][A-Za-z0-9_-]*$"), HomeSectionPayload.SectionId);
    }

    [Fact]
    public void Build_HasNoRouteSoTheHeaderIsNotALink()
    {
        var payload = HomeSectionPayload.Build("asm");
        Assert.False(payload.ContainsKey("route"));
        Assert.False(payload.ContainsKey("additionalData"));
        Assert.False(payload.ContainsKey("resultsEndpoint"));
    }

    [Fact]
    public void ToJson_RoundTripsWithCamelCaseKeysAndNumericLimit()
    {
        var json = HomeSectionPayload.ToJson("asm");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("limit").GetInt32());
        Assert.Equal("Watchlist", doc.RootElement.GetProperty("displayText").GetString());
        Assert.Equal(HomeSectionPayload.SectionId, doc.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public void TranslationPackJson_MapsTheSectionIdToTheDisplayTextInEnglish()
    {
        using var doc = JsonDocument.Parse(HomeSectionPayload.TranslationPackJson());
        Assert.Single(doc.RootElement.EnumerateObject());
        Assert.Equal(HomeSectionPayload.DisplayText, doc.RootElement.GetProperty(HomeSectionPayload.SectionId).GetString());
        Assert.Equal("en", HomeSectionPayload.TranslationLanguage);
    }
}
