using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin? Instance { get; private set; }

        private readonly ILogger<Plugin> _logger;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;
            _logger.LogInformation("Watchlist v{Version} initialized", Version);
        }

        public override string Name => "Watchlist";

        public override Guid Id => Guid.Parse("06dd4cfd-82bc-401c-9fde-6769ed8c4032");

        public override string Description =>
            "Watchlist page and add/remove buttons for jellyfin-web, backed by Jellyfin's native Likes user data.";

        /// <summary>
        /// Cache-buster for the client script URL: version plus the DLL write time,
        /// so every build gets a new URL and the bundle can be cached immutably.
        /// </summary>
        public string ScriptCacheKey
        {
            get
            {
                var version = Version?.ToString() ?? "0";
                try
                {
                    var location = typeof(Plugin).Assembly.Location;
                    if (!string.IsNullOrEmpty(location) && File.Exists(location))
                    {
                        return $"{version}-{File.GetLastWriteTimeUtc(location).Ticks}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not read assembly write time; using bare version");
                }

                return version;
            }
        }

        /// <summary>The script tag inserted into index.html. Relative src keeps base-url installs working.</summary>
        public string BuildScriptTag() =>
            $"<script plugin=\"{Name}\" version=\"{ScriptCacheKey}\" src=\"../Watchlist/script?v={ScriptCacheKey}\" defer></script>";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    DisplayName = "Watchlist",
                    EnableInMainMenu = true,
                    MenuIcon = "bookmark",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }
    }
}
