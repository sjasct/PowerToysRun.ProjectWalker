using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FuzzySharp;
using ManagedCommon;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject
{
    /// <summary>
    /// Main class of this plugin that implement all used interfaces.
    /// </summary>
    public class Main : IPlugin, IContextMenu, IDisposable
    {
        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "2dbbb8424c3e47edb45ae3253fd67b82";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "OpenProject";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "PowerToysRun.OpenProject Description";

        private PluginInitContext Context { get; set; }
        
        private bool Disposed { get; set; }
        
        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrWhiteSpace(ConfigHelper.Instance.Config.BasePath))
            {
                return [new()
                {
                    IcoPath = ConfigHelper.Instance.GetBaseIconPath(),
                    Title = "Please set base path"
                }];
            }

            if (query.Search.StartsWith("-o"))
            {
                return GenerateProjectOpenResults(query);
            }

            return GenerateRepositorySearchResults(query);
        }

        private List<Result> GenerateRepositorySearchResults(Query query)
        {
            var search = query.Search;

            var topLevels = Directory.GetDirectories(ConfigHelper.Instance.Config.BasePath);
            var folders = new List<Folder>();
            
            foreach (var topLevel in topLevels)
            {
                var repoDirs = Directory.GetDirectories(topLevel);
                foreach (var repoDir in repoDirs)
                {
                    folders.Add(new Folder(new DirectoryInfo(topLevel).Name, new DirectoryInfo(repoDir).Name));
                }
            }

            var folderResults = folders.Where(x => string.IsNullOrWhiteSpace(query.Search) || Fuzz.PartialRatio($"{x.ProjectFolderName}/{x.RepoFolderName}", search) > 75);
            return folderResults.Select(x => new Result()
            {
                QueryTextDisplay = search,
                IcoPath = ConfigHelper.Instance.GetBaseIconPath(),
                Title = x.RepoFolderName,
                SubTitle = x.ProjectFolderName,
                Action = _ =>
                {
                    Context.API.ChangeQuery($"{query.ActionKeyword} -o \"{x.ProjectFolderName}\\{x.RepoFolderName}\"", true);
                    return false;
                },
                ContextData = search,
            }).ToList();
        }

        private record Folder(string ProjectFolderName, string RepoFolderName);

        private List<Result> GenerateProjectOpenResults(Query query)
        {
            var path = Path.Combine(ConfigHelper.Instance.Config.BasePath, query.Search.Replace("-o", string.Empty).Replace("\"", string.Empty).Trim());

            if (!Path.Exists(path))
            {
                return [GetErrorResult("Could not find path")];
            }

            if (!ConfigHelper.Instance.Config.Options.Any())
            {
                return [GetErrorResult("No open options have been set")];
            }
            
            var results = new List<Result>();
            var builder = new OpenOptionBuilder();

            foreach (var option in ConfigHelper.Instance.Config.Options.OrderBy(x => x.Index))
            {
                var result = option.Type switch
                {
                    "process" => builder.BuildProcessResult(option, query, path),
                    "browser" => builder.BuildBrowserResult(option, query, path),
                    "clipboard" => builder.BuildClipboardResult(option, query, path),
                    _ => null
                };

                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            ConfigHelper.Instance.SetTheme(Context.API.GetCurrentTheme());
            ConfigHelper.Instance.LoadConfig();
        }

        private Result GetErrorResult(string message)
        {
            return new Result()
            {
                IcoPath = ConfigHelper.Instance.GetBaseIconPath(),
                Title = message
            };
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is string search)
            {
                return
                [
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Copy to clipboard (Ctrl+C)",
                        FontFamily = "Segoe MDL2 Assets",
                        Glyph = "\xE8C8", // Copy
                        AcceleratorKey = Key.C,
                        AcceleratorModifiers = ModifierKeys.Control,
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(search);
                            return true;
                        },
                    }
                ];
            }

            return [];
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose()"/> that dispose additional objects and events form the plugin itself.
        /// </summary>
        /// <param name="disposing">Indicate that the plugin is disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            if (Context?.API != null)
            {
                Context.API.ThemeChanged -= OnThemeChanged;
            }

            Disposed = true;
        }
        
        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => ConfigHelper.Instance.SetTheme(newTheme);
    }
}