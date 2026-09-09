using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Watchlist.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Watchlist.Tests;

public class RemovalPolicyTests
{
    [Theory]
    [InlineData(UserDataSaveReason.PlaybackFinished)]
    [InlineData(UserDataSaveReason.TogglePlayed)]
    [InlineData(UserDataSaveReason.UpdateUserData)]
    [InlineData(UserDataSaveReason.Import)]
    public void Qualifies_WhenPlayedAndReasonIsAPlayedTransition(UserDataSaveReason reason)
    {
        Assert.True(RemovalPolicy.Qualifies(reason, true));
    }

    [Theory]
    [InlineData(UserDataSaveReason.PlaybackStart)]
    [InlineData(UserDataSaveReason.PlaybackProgress)]
    [InlineData(UserDataSaveReason.UpdateUserRating)]
    public void DoesNotQualify_ForNonPlayedReasonsEvenWhenPlayed(UserDataSaveReason reason)
    {
        Assert.False(RemovalPolicy.Qualifies(reason, true));
    }

    [Fact]
    public void DoesNotQualify_WhenNotPlayed()
    {
        foreach (var reason in Enum.GetValues<UserDataSaveReason>())
        {
            Assert.False(RemovalPolicy.Qualifies(reason, false));
            Assert.False(RemovalPolicy.Qualifies(reason, null));
        }
    }

    [Fact]
    public void CandidateKinds_FollowTheSpecOrder()
    {
        Assert.Equal(new[] { BaseItemKind.Movie }, RemovalPolicy.CandidateKinds(BaseItemKind.Movie));
        Assert.Equal(new[] { BaseItemKind.Episode, BaseItemKind.Season, BaseItemKind.Series }, RemovalPolicy.CandidateKinds(BaseItemKind.Episode));
        Assert.Equal(new[] { BaseItemKind.Season, BaseItemKind.Series }, RemovalPolicy.CandidateKinds(BaseItemKind.Season));
        Assert.Equal(new[] { BaseItemKind.Series }, RemovalPolicy.CandidateKinds(BaseItemKind.Series));
    }

    [Theory]
    [InlineData(BaseItemKind.Audio)]
    [InlineData(BaseItemKind.BoxSet)]
    [InlineData(BaseItemKind.Playlist)]
    [InlineData(BaseItemKind.Video)]
    public void CandidateKinds_IsEmptyForNonWatchlistKinds(BaseItemKind kind)
    {
        Assert.Empty(RemovalPolicy.CandidateKinds(kind));
    }

    [Fact]
    public void NeedsEpisodeCheck_OnlyForParents()
    {
        Assert.True(RemovalPolicy.NeedsEpisodeCheck(BaseItemKind.Season));
        Assert.True(RemovalPolicy.NeedsEpisodeCheck(BaseItemKind.Series));
        Assert.False(RemovalPolicy.NeedsEpisodeCheck(BaseItemKind.Movie));
        Assert.False(RemovalPolicy.NeedsEpisodeCheck(BaseItemKind.Episode));
    }

    [Fact]
    public void EnabledKinds_MapsFlagsInOrder()
    {
        Assert.Equal(
            new[] { BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Season, BaseItemKind.Episode },
            RemovalPolicy.EnabledKinds(true, true, true, true));
        Assert.Equal(new[] { BaseItemKind.Series, BaseItemKind.Episode }, RemovalPolicy.EnabledKinds(false, true, false, true));
        Assert.Empty(RemovalPolicy.EnabledKinds(false, false, false, false));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(16, 16)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(-5, 1)]
    public void ClampLimit_KeepsOneToHundred(int input, int expected)
    {
        Assert.Equal(expected, RemovalPolicy.ClampLimit(input));
    }
}
