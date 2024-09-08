using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Template.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;

namespace Jellyfin.Plugin.Template
{
    using Emby.Server.Implementations.Library;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/plugins")]
    [ApiController]
    public class PluginController : ControllerBase
    {

        private readonly ILibraryManager _libraryManager;
        public PluginController(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }
        [HttpGet("test")]
        public ActionResult<string> TestEndpoint()
        {
            return new ContentResult
            {
                Content = "ok",
                ContentType = "text/plain",
                StatusCode = 200
            };
        }

        [HttpGet("delete-duplicates")]
        public async Task<IActionResult> TriggerDeleteDuplicateMovies(string test)
        {
            // Call the asynchronous deletion method and wait for completion
            var result = await DeleteDuplicateMoviesAsync(test.ToLower() != "false");
            _libraryManager.QueueLibraryScan();

            return new ContentResult
            {
                Content = result,
                ContentType = "text/plain",
                StatusCode = 200
            };
        }
        private async Task<string> DeleteDuplicateMoviesAsync(bool test)
        {
            string ret = "";
            try
            {
                var movies = _libraryManager.GetItemsResult(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie }
                }).Items;

                var groupedMovies = movies
                    .Where(m => m.ProviderIds.ContainsKey("Imdb"))
                    .GroupBy(m => m.ProviderIds["Imdb"]);

                foreach (var group in groupedMovies)
                {
                    var sortedMovies = group.OrderByDescending(m => m.GetMediaStreams().FirstOrDefault()?.Height ?? 0)
                                            .ThenByDescending(m => m.GetMediaStreams().FirstOrDefault()?.BitRate ?? 0)
                                            .ThenByDescending(m => new FileInfo(m.Path).Length)
                                            .ToList();
                    string msg = "";
                    foreach (var movie in sortedMovies.Skip(1))
                    {
                        try
                        {

                            if (!test)
                                System.IO.File.Delete(movie.Path);
                            msg = $"File deleted {movie.Path}.{movie.ProviderIds["Imdb"]}";

                            ret += msg + "\r\n";

                            var folder = new DirectoryInfo(Path.GetDirectoryName(movie.Path));
                            if (GetDirectorySize(folder) < 20 * 1024 * 1024)
                            {
                                folder.Delete(true);
                                msg = $"Folder deleted {folder.FullName}.";


                                ret += msg + "\r\n";
                            }
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ;
            }
            return ret;
        }

        private long GetDirectorySize(DirectoryInfo folder)
        {
            return folder.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

    }

    /// <summary>
    /// The main plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{Plugin}"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            _libraryManager = libraryManager;
            Instance = this;
        }

        /// <inheritdoc />
        public override string Name => "JellyfinDuplicateFinderr";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce4");

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }




        public IEnumerable<PluginPageInfo> GetPages()
        {
            var assembly = GetType().Assembly;

            // Helper function to find the correct resource name
            string GetResourceName(string pageName, string extension)
            {
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith($"{pageName}.{extension}", true, CultureInfo.InvariantCulture));

                if (resourceName == null)
                    throw new System.Exception($"Resource for page '{pageName}' with extension '{extension}' not found.");

                return resourceName;
            }

            return new[]
            {
                new PluginPageInfo
                {
                    Name = "duplicateindex",
                    EmbeddedResourcePath = GetResourceName("duplicateindex", "html")
                }
            };
        }


    }
}
