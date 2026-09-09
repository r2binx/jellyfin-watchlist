using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Watchlist.HomeSection
{
    /// <summary>
    /// Gives Home Screen Sections a label for the Watchlist row. Its user settings page translates every
    /// section name through its own translation table and falls back to the literal "Genre Section" for
    /// unknown keys, so without an entry our checkbox is mislabelled. Adding the id to its English pack fixes
    /// every language: the translator falls back to the English pack when a language lacks the key.
    /// </summary>
    public static class HomeSectionTranslation
    {
        public const string ManagerType = "Jellyfin.Plugin.HomeScreenSections.Library.ITranslationManager";
        public const string UpdateMethod = "UpdateTranslationPack";

        /// <summary>Returns true when the label was added. Returns false when the plugin or its translation manager is absent.</summary>
        public static bool TryRegisterLabel(IServiceProvider services, ILogger logger)
        {
            var managerType = HomeSectionRegistration.FindAssembly()?.GetType(ManagerType);
            var manager = managerType == null ? null : services.GetService(managerType);
            var update = managerType?.GetMethod(UpdateMethod, BindingFlags.Public | BindingFlags.Instance);
            if (manager == null || update == null)
            {
                return false;
            }

            var parameters = update.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(string))
            {
                throw new InvalidOperationException($"Watchlist: Home Screen Sections {UpdateMethod} has an unexpected signature");
            }

            var pack = HomeSectionRegistration.ParseWith(parameters[1].ParameterType, HomeSectionPayload.TranslationPackJson());
            update.Invoke(manager, new[] { HomeSectionPayload.TranslationLanguage, pack });
            logger.LogInformation("Watchlist: added the home row label to Home Screen Sections");
            return true;
        }
    }
}
