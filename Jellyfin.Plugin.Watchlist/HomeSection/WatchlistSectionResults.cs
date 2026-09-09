using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Watchlist.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>
    /// Results handler invoked by Home Screen Sections for the Watchlist row. Instantiated by that plugin
    /// through Jellyfin's service provider; the class and method names are fixed in HomeSectionPayload.
    /// </summary>
    public sealed class WatchlistSectionResults
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger<WatchlistSectionResults> _logger;

        public WatchlistSectionResults(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService,
            ILogger<WatchlistSectionResults> logger)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
        }

        public QueryResult<BaseItemDto> GetResults(WatchlistSectionRequest request)
        {
            var userId = request?.UserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                return new QueryResult<BaseItemDto>();
            }

            try
            {
                var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
                if (!config.EnableHomeSection)
                {
                    return new QueryResult<BaseItemDto>();
                }

                var user = _userManager.GetUserById(userId);
                if (user == null)
                {
                    return new QueryResult<BaseItemDto>();
                }

                var kinds = RemovalPolicy.EnabledKinds(config.ShowMovies, config.ShowSeries, config.ShowSeasons, config.ShowEpisodes);
                if (kinds.Count == 0)
                {
                    return new QueryResult<BaseItemDto>();
                }

                var dtoOptions = new DtoOptions
                {
                    Fields = new[] { ItemFields.PrimaryImageAspectRatio },
                    ImageTypeLimit = 1,
                    ImageTypes = new[] { ImageType.Thumb, ImageType.Backdrop, ImageType.Primary },
                };

                var query = new InternalItemsQuery(user)
                {
                    IsLiked = true,
                    Recursive = true,
                    IncludeItemTypes = kinds.ToArray(),
                    OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                    Limit = RemovalPolicy.ClampLimit(config.HomeSectionLimit),
                    DtoOptions = dtoOptions,
                };

                var result = _libraryManager.GetItemsResult(query);
                var dtos = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user);
                return new QueryResult<BaseItemDto>(0, result.TotalRecordCount, dtos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Watchlist: home row query failed for user {UserId}", userId);
                return new QueryResult<BaseItemDto>();
            }
        }
    }
}
