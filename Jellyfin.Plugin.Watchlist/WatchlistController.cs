using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Watchlist
{
    [ApiController]
    [Route("Watchlist")]
    public class WatchlistController : ControllerBase
    {
        private static readonly string[] BundleOrder = { "core.js", "page.js", "detail-button.js", "card-overlay.js" };
        private static readonly Lazy<string> Bundle = new(BuildBundle);

        /// <summary>Client bundle. Anonymous: index.html loads it before login.</summary>
        [HttpGet("script")]
        public ActionResult GetScript()
        {
            Response.Headers["Cache-Control"] = Request.Query.ContainsKey("v")
                ? "public, max-age=31536000, immutable"
                : "no-cache";
            return Content(Bundle.Value, "application/javascript; charset=utf-8");
        }

        [HttpGet("version")]
        public ActionResult GetVersion() => Content(Plugin.Instance?.Version?.ToString() ?? "unknown", "text/plain");

        [Authorize]
        [HttpGet("config")]
        public ActionResult GetConfig()
        {
            var c = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            return Ok(new Dictionary<string, bool>
            {
                ["ShowMovies"] = c.ShowMovies,
                ["ShowSeries"] = c.ShowSeries,
                ["ShowSeasons"] = c.ShowSeasons,
                ["ShowEpisodes"] = c.ShowEpisodes,
                ["EnableCardOverlayButtons"] = c.EnableCardOverlayButtons,
            });
        }

        /// <summary>Page HTML fetched by Plugin Pages (with the user's token) into its container.</summary>
        [Authorize]
        [HttpGet("page")]
        public ActionResult GetPage()
        {
            var html = ReadResource("Pages.WatchlistPage.html");
            if (html == null)
            {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = "no-cache";
            return Content(html, "text/html; charset=utf-8");
        }

        private static string BuildBundle()
        {
            var sb = new StringBuilder();
            var css = ReadResource("Web.watchlist.css") ?? string.Empty;
            // Inject the stylesheet from JS so a single script tag is the only thing index.html needs.
            sb.Append("(function(){if(document.getElementById('jfw-style'))return;var s=document.createElement('style');s.id='jfw-style';s.textContent=")
              .Append(JsonSerializer.Serialize(css))
              .Append(";document.head.appendChild(s);})();\n");
            foreach (var name in BundleOrder)
            {
                sb.Append("\n/* ---- ").Append(name).Append(" ---- */\n");
                sb.Append(ReadResource("Web." + name) ?? string.Empty).Append('\n');
            }

            return sb.ToString();
        }

        private static string? ReadResource(string suffix)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"Jellyfin.Plugin.Watchlist.{suffix}");
            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
