using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.Services
{
    /// <summary>
    /// Clears the Likes flag when a watchlisted item is played. Movies and episodes leave when they
    /// themselves are played; seasons and series leave once none of their episodes is unplayed.
    /// </summary>
    public sealed class PlayedWatchlistRemover
    {
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly ILogger<PlayedWatchlistRemover> _logger;

        public PlayedWatchlistRemover(
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            ILogger<PlayedWatchlistRemover> logger)
        {
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _logger = logger;
        }

        public void Subscribe() => _userDataManager.UserDataSaved += OnUserDataSaved;

        public void Unsubscribe() => _userDataManager.UserDataSaved -= OnUserDataSaved;

        private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
        {
            try
            {
                if (Plugin.Instance?.Configuration.AutoRemovePlayed != true)
                {
                    return;
                }

                if (!RemovalPolicy.Qualifies(e.SaveReason, e.UserData?.Played))
                {
                    return;
                }

                var item = e.Item;
                if (item == null || e.UserId == Guid.Empty)
                {
                    return;
                }

                var kinds = RemovalPolicy.CandidateKinds(item.GetBaseItemKind());
                if (kinds.Count == 0)
                {
                    return;
                }

                var user = _userManager.GetUserById(e.UserId);
                if (user == null)
                {
                    return;
                }

                foreach (var candidate in Resolve(item, kinds))
                {
                    TryRemove(user, candidate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Watchlist: auto-removal check failed for item {ItemId}", e.Item?.Id);
            }
        }

        /// <summary>Resolves the entity for each kind RemovalPolicy.CandidateKinds returns, skipping missing parents.</summary>
        private static IEnumerable<BaseItem> Resolve(BaseItem item, IReadOnlyList<BaseItemKind> kinds)
        {
            foreach (var kind in kinds)
            {
                var entity = EntityFor(item, kind);
                if (entity != null)
                {
                    yield return entity;
                }
            }
        }

        // This mapping must cover every kind RemovalPolicy.CandidateKinds can return.
        private static BaseItem? EntityFor(BaseItem item, BaseItemKind kind)
        {
            if (kind == item.GetBaseItemKind())
            {
                return item;
            }

            return item switch
            {
                Episode episode when kind == BaseItemKind.Season => episode.Season,
                Episode episode when kind == BaseItemKind.Series => episode.Series,
                Season season when kind == BaseItemKind.Series => season.Series,
                _ => null,
            };
        }

        /// <summary>
        /// A Season or Series is checked for unplayed episodes even when it is itself the played item, because
        /// Jellyfin's hand-marked cascade may not have committed the episodes yet and the last episode event
        /// removes the parent anyway.
        /// </summary>
        private void TryRemove(User user, BaseItem candidate)
        {
            var userData = _userDataManager.GetUserData(user, candidate);
            if (userData?.Likes != true)
            {
                return;
            }

            var kind = candidate.GetBaseItemKind();
            if (RemovalPolicy.NeedsEpisodeCheck(kind) && HasUnplayedEpisodes(user, candidate))
            {
                return;
            }

            userData.Likes = null;
            _userDataManager.SaveUserData(user, candidate, userData, UserDataSaveReason.UpdateUserRating, CancellationToken.None);
            _logger.LogInformation("Watchlist: removed {Kind} '{Name}' from {User}'s watchlist after playback", kind, candidate.Name, user.Username);
        }

        private bool HasUnplayedEpisodes(User user, BaseItem parent)
        {
            var query = new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                IsPlayed = false,
                IsVirtualItem = false,
                Recursive = true,
                Limit = 1,
            };
            return _libraryManager.GetItemIds(query).Count > 0;
        }
    }
}
