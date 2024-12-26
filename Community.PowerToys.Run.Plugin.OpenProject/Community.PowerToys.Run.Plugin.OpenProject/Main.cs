using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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

        private string IconPath { get; set; }

        private bool Disposed { get; set; }
        
        // todo: setting
        private static string _basePath = "C:\\.sjas\\dev"; 

        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrWhiteSpace(_basePath))
            {
                return [new()
                {
                    IcoPath = IconPath,
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

            var topLevels = Directory.GetDirectories(_basePath);
            var results = new List<Result>();
            foreach (var topLevel in topLevels)
            {
                var repoDirs = Directory.GetDirectories(topLevel, $"*{search}*");
                results.AddRange(repoDirs.Select(x => new Result()
                {
                    QueryTextDisplay = search,
                    IcoPath = IconPath,
                    Title = new DirectoryInfo(x).Name,
                    SubTitle = new DirectoryInfo(topLevel).Name,
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} -o \"{x}\"", true);
                        return false;
                    },
                    ContextData = search,
                }));
            }

            return results;
        }

        private List<Result> GenerateProjectOpenResults(Query query)
        {
            return [new Result()
            {
                QueryTextDisplay = query.Search,
                IcoPath = IconPath,
                Title = "DEBUG - todo open options",
                ToolTipData = new ToolTipData("Title", "Text"),
                ContextData = query.Search,
            }];
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
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

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
            ? "Images/powertoysrun.openproject.light.png"
            : "Images/powertoysrun.openproject.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
    }
}