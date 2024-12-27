using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.Models;
using LibGit2Sharp;
using ManagedCommon;
using Wox.Plugin;
using Configuration = Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.Models.Configuration;
using Helper = Wox.Infrastructure.Helper;

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

        private Configuration _config = null!;

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
                var topDir = new DirectoryInfo(topLevel).Name;
                results.AddRange(repoDirs.Select(x => new Result()
                {
                    QueryTextDisplay = search,
                    IcoPath = IconPath,
                    Title = new DirectoryInfo(x).Name,
                    SubTitle = topDir,
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} -o \"{topDir}\\{new DirectoryInfo(x).Name}\"", true);
                        return false;
                    },
                    ContextData = search,
                }));
            }

            return results;
        }

        private List<Result> GenerateProjectOpenResults(Query query)
        {
            var path = Path.Combine(_basePath, query.Search.Replace("-o", string.Empty).Replace("\"", string.Empty).Trim());

            if (!Path.Exists(path))
            {
                return [GetErrorResult("Could not find path")];
            }

            if (!_config.Options.Any())
            {
                return [GetErrorResult("No open options have been set")];
            }
            
            var results = new List<Result>();

            foreach (var option in _config.Options.OrderBy(x => x.Index))
            {
                if (option.Type == "process")
                {
                    if (string.IsNullOrWhiteSpace(option.ProcessName))
                    {
                        return [GetErrorResult("Process name is required")];
                    }

                    var flags = option.Arguments?.Trim();

                    if (!string.IsNullOrWhiteSpace(flags))
                    {
                        flags = flags.Replace("{{PATH}}", path);

                        // FILE MATCHES
                        // this is a very hacky solution, just testing if it works
                        // todo: if multiple results are found, pawn the user off to a seperate menu to pick which file?? idk
                        var fileExtMatches = Regex.Matches(flags, "{{FILE:(.+)}}");
                        if (fileExtMatches.Any())
                        {
                            var ext = fileExtMatches.First().Groups[1];
                            var fileResults = new DirectoryInfo(path).GetFiles($"{ext.Value}");

                            if (fileResults.Any())
                            {
                                flags = flags.Replace(fileExtMatches.First().Value, fileResults[0].FullName);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        
                        // GIT
                        // this is also a hack
                        var gitMatches = Regex.Matches(flags, "{{GIT:(.+)}}");
                        if (gitMatches.Any())
                        {
                            if (Repository.IsValid(path))
                            {
                                using var gitRepo = new Repository(path);
                                foreach (var gitMatch in gitMatches.DistinctBy(x => x.Groups[1].Value))
                                {
                                    var gitMatchKey = gitMatch.Groups[1].Value;
                                    if (gitMatchKey == "REMOTE_URL" && gitRepo.Network.Remotes.Any())
                                    {
                                        flags = flags.Replace(gitMatch.Value, gitRepo.Network.Remotes.First().Url);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    
                    results.Add(new Result()
                    {
                        QueryTextDisplay = query.Search,
                        IcoPath = IconPath,
                        Title = option.Name,
                        SubTitle = $"{option.ProcessName} {flags}",
                        Action = _ =>
                        {
                            Helper.OpenInShell(option.ProcessName, flags);
                            return true;
                        },
                        ContextData = query.Search,
                        Score = _config.Options.Max(x => x.Index) - option.Index
                    });
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
            UpdateIconPath(Context.API.GetCurrentTheme());

            var configFile = Path.Combine(_basePath, "power-toys-open-project-config.json");

            if (!File.Exists(configFile))
            {
                _config = GetDefaultConfiguration();
                File.WriteAllText(configFile, JsonSerializer.Serialize(_config, JsonSerializerOptions.Web));
                return;
            }

            var fullConfig =
                JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFile), JsonSerializerOptions.Web);

            if (fullConfig == null)
            {
                Logger.LogError("Failed to deserialize config");
                throw new Exception("Failed to load config");
            }

            _config = fullConfig;
        }

        private Result GetErrorResult(string message)
        {
            return new Result()
            {
                IcoPath = IconPath,
                Title = message
            };
        }

        private Configuration GetDefaultConfiguration()
        {
            return new Configuration()
            {
                Options =
                [
                    new OpenOption()
                    {
                        Type = "process",
                        Name = "Explorer",
                        Index = 0,
                        ProcessName = "explorer",
                        Arguments = "{{PATH}}"
                    },
                    new OpenOption()
                    {
                        Type = "process",
                        Name = "VS Code",
                        Index = 1,
                        ProcessName = "code",
                        Arguments = "{{PATH}}"
                    },
                    new OpenOption()
                    {
                        Type = "process",
                        Name = "Rider",
                        Index = 2,
                        ProcessName = "rider",
                        Arguments = "{{FILE:*.sln}}"
                    },
                ]
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

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
            ? "Images/powertoysrun.openproject.light.png"
            : "Images/powertoysrun.openproject.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
    }
}