using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>
    /// Registers the Watchlist row with the Home Screen Sections plugin through its static
    /// <c>PluginInterface.RegisterSection(JObject)</c>. The payload is parsed with that plugin's own JObject
    /// type so the call does not fail on a cross-load-context type mismatch (same trick as PluginPagesRegistration).
    /// </summary>
    public static class HomeSectionRegistration
    {
        public const string AssemblyName = "Jellyfin.Plugin.HomeScreenSections";
        public const string InterfaceType = "Jellyfin.Plugin.HomeScreenSections.PluginInterface";
        public const string RegisterMethod = "RegisterSection";

        /// <summary>Returns true when the section is registered (now or already). Throws when the plugin is present but the call fails.</summary>
        public static bool TryRegister(ILogger logger)
        {
            var assembly = AssemblyLoadContext.All
                .SelectMany(c => c.Assemblies)
                .FirstOrDefault(a => a.GetName().Name == AssemblyName);
            var register = assembly?.GetType(InterfaceType)?.GetMethod(RegisterMethod, BindingFlags.Public | BindingFlags.Static);
            if (register == null)
            {
                return false;
            }

            var payloadType = register.GetParameters()[0].ParameterType;
            var parse = payloadType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
            if (parse == null)
            {
                logger.LogWarning("Watchlist: Home Screen Sections RegisterSection has an unexpected parameter type {Type}", payloadType.FullName);
                return false;
            }

            var resultsAssembly = typeof(HomeSectionRegistration).Assembly.FullName ?? "Jellyfin.Plugin.Watchlist";
            var payload = parse.Invoke(null, new object[] { HomeSectionPayload.ToJson(resultsAssembly) });

            try
            {
                register.Invoke(null, new[] { payload });
            }
            catch (TargetInvocationException ex) when (ex.InnerException?.Message.Contains("already been registered", StringComparison.OrdinalIgnoreCase) == true)
            {
                logger.LogDebug("Watchlist: home section was already registered");
                return true;
            }

            logger.LogInformation("Watchlist: registered home row with Home Screen Sections");
            return true;
        }
    }
}
