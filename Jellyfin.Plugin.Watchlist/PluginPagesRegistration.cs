using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist
{
    /// <summary>
    /// Registers the Watchlist page with the Plugin Pages plugin.
    /// Persistent: writes an entry into Plugin Pages' config.json (read on its next start).
    /// Immediate (best effort): calls Plugin Pages' static PluginInterface.RegisterPage via
    /// reflection so the sidebar entry appears without a second restart.
    /// </summary>
    public static class PluginPagesRegistration
    {
        public const string PageId = "Jellyfin.Plugin.Watchlist.WatchlistPage";
        private const string PageUrl = "/Watchlist/page";

        public static void Register(IApplicationPaths paths, ILogger logger)
        {
            try
            {
                var (file, config, pages) = Load(paths);
                if (!pages.Any(p => p?["Id"]?.GetValue<string>() == PageId))
                {
                    pages.Add(NewEntry());
                    Save(file, config);
                    logger.LogInformation("Watchlist: added page entry to Plugin Pages config.json");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Watchlist: could not write Plugin Pages config.json; the sidebar entry will be missing");
            }

            TryRegisterLive(logger);
        }

        public static void Remove(IApplicationPaths paths, ILogger logger)
        {
            try
            {
                var (file, config, pages) = Load(paths);
                var removed = 0;
                for (var i = pages.Count - 1; i >= 0; i--)
                {
                    if (pages[i]?["Id"]?.GetValue<string>() == PageId)
                    {
                        pages.RemoveAt(i);
                        removed++;
                    }
                }

                if (removed > 0)
                {
                    Save(file, config);
                    logger.LogInformation("Watchlist: removed page entry from Plugin Pages config.json");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Watchlist: could not update Plugin Pages config.json on uninstall");
            }
        }

        private static JsonObject NewEntry() => new JsonObject
        {
            ["Id"] = PageId,
            ["Url"] = PageUrl,
            ["DisplayText"] = "Watchlist",
            ["Icon"] = "bookmark",
        };

        private static (string file, JsonObject config, JsonArray pages) Load(IApplicationPaths paths)
        {
            var dir = Path.Combine(paths.PluginConfigurationsPath, "Jellyfin.Plugin.PluginPages");
            var file = Path.Combine(dir, "config.json");
            Directory.CreateDirectory(dir);

            JsonObject config = new JsonObject();
            if (File.Exists(file))
            {
                config = JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject();
            }

            if (config["pages"] is not JsonArray pages)
            {
                pages = new JsonArray();
                config["pages"] = pages;
            }

            return (file, config, pages);
        }

        private static void Save(string file, JsonObject config)
        {
            File.WriteAllText(file, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void TryRegisterLive(ILogger logger)
        {
            try
            {
                var assembly = AssemblyLoadContext.All
                    .SelectMany(c => c.Assemblies)
                    .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.PluginPages");
                var register = assembly?.GetType("Jellyfin.Plugin.PluginPages.PluginInterface")?.GetMethod("RegisterPage", BindingFlags.Public | BindingFlags.Static);
                if (register == null)
                {
                    return; // Plugin Pages not loaded (yet); config.json covers the next start.
                }

                // Build the payload with Plugin Pages' own JObject type (its Newtonsoft assembly),
                // so the reflected call does not fail on a cross-load-context type mismatch.
                var payloadType = register.GetParameters()[0].ParameterType;
                var parse = payloadType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
                var payload = parse?.Invoke(null, new object[] { NewEntry().ToJsonString() });
                if (payload != null)
                {
                    register.Invoke(null, new[] { payload });
                    logger.LogInformation("Watchlist: registered page with Plugin Pages at runtime");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Watchlist: live Plugin Pages registration skipped");
            }
        }
    }
}
