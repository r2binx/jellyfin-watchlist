using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.Services
{
    /// <summary>Starts the event listener once Jellyfin is up and stops it on shutdown.</summary>
    public sealed class WatchlistHostedService : IHostedService
    {
        private readonly PlayedWatchlistRemover _remover;
        private readonly ILogger<WatchlistHostedService> _logger;

        public WatchlistHostedService(PlayedWatchlistRemover remover, ILogger<WatchlistHostedService> logger)
        {
            _remover = remover;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _remover.Subscribe();
            _logger.LogInformation("Watchlist: auto-removal listener attached");
            // TODO: add home-section registration here
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _remover.Unsubscribe();
            return Task.CompletedTask;
        }
    }
}
