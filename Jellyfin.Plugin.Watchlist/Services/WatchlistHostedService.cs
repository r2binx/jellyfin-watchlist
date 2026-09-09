using Jellyfin.Plugin.Watchlist.HomeSection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.Services
{
    /// <summary>
    /// Starts the event listener once Jellyfin is up, registers the home row with Home Screen Sections
    /// (retrying briefly in case that plugin initialises after us), and detaches on shutdown.
    /// </summary>
    public sealed class WatchlistHostedService : IHostedService
    {
        public const int RegistrationAttempts = 5;
        public static readonly TimeSpan RegistrationDelay = TimeSpan.FromSeconds(2);

        private readonly PlayedWatchlistRemover _remover;
        private readonly IServiceProvider _services;
        private readonly ILogger<WatchlistHostedService> _logger;
        private readonly CancellationTokenSource _stopping = new();

        public WatchlistHostedService(PlayedWatchlistRemover remover, IServiceProvider services, ILogger<WatchlistHostedService> logger)
        {
            _remover = remover;
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _remover.Subscribe();
            _logger.LogInformation("Watchlist: auto-removal listener attached");
            _ = Task.Run(() => RegisterHomeSectionAsync(_stopping.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping.Cancel();
            _remover.Unsubscribe();
            return Task.CompletedTask;
        }

        private async Task RegisterHomeSectionAsync(CancellationToken token)
        {
            Exception? lastFailure = null;

            for (var attempt = 1; attempt <= RegistrationAttempts && !token.IsCancellationRequested; attempt++)
            {
                try
                {
                    if (HomeSectionRegistration.TryRegister(_logger))
                    {
                        RegisterLabel();
                        return;
                    }

                    _logger.LogDebug("Watchlist: Home Screen Sections not available on attempt {Attempt}", attempt);
                }
                catch (Exception ex)
                {
                    lastFailure = ex;
                    _logger.LogDebug(ex, "Watchlist: home section registration failed on attempt {Attempt}", attempt);
                }

                try
                {
                    await Task.Delay(RegistrationDelay, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (lastFailure != null)
            {
                _logger.LogWarning(lastFailure, "Watchlist: Home Screen Sections is installed but registering the home row failed; the row will be missing until the next restart");
            }
            else
            {
                _logger.LogInformation("Watchlist: Home Screen Sections not found; home row unavailable");
            }
        }

        private void RegisterLabel()
        {
            try
            {
                if (!HomeSectionTranslation.TryRegisterLabel(_services, _logger))
                {
                    _logger.LogWarning("Watchlist: Home Screen Sections has no translation manager; its settings page will mislabel the Watchlist checkbox");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Watchlist: adding the home row label to Home Screen Sections failed; its settings page will mislabel the Watchlist checkbox");
            }
        }
    }
}
