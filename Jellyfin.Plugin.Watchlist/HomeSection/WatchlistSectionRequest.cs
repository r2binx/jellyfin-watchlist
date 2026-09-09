namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>Deserialisation target for Home Screen Sections' per-user results call.</summary>
    public sealed class WatchlistSectionRequest
    {
        public Guid UserId { get; set; }

        public string? AdditionalData { get; set; }
    }
}
