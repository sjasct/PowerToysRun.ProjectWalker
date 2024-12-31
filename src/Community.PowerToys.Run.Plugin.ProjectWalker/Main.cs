using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;
using FuzzySharp;
using ManagedCommon;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker
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
        public string Name => "ProjectWalker";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Quickly search and open projects in specific applications";

        private PluginInitContext Context { get; set; }
        
        private bool Disposed { get; set; }
        
        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrWhiteSpace(ConfigService.Instance.Config.BasePath))
            {
                var results = GenerateConfigManagementResults(query);
                results.Add(new()
                {
                    Title = "Please set base path in config.json",
                    IcoPath = IconHelper.GetIconPath("error"),
                    Score = 9999
                });

                return results;
            }

            if (query.Search.ToLower().StartsWith("-o"))
            {
                return GenerateProjectOpenResults(query);
            }

            if (query.Search.ToLower().StartsWith("-c"))
            {
                return GenerateConfigManagementResults(query);
            }

            return ConfigService.Instance.Config.FolderStructureType switch
            {
                FolderStructureType.ProjectParents => GenerateProjectParentSearchResults(query),
                FolderStructureType.StandaloneRepos => GenerateStandaloneRepoSearchResults(query),
                _ => throw new ConfigurationErrorsException("")
            };
        }

        private List<Result> GenerateConfigManagementResults(Query query)
        {
            List<Result> results = [
                new()
                {
                    Title = "Edit Config in Notepad",
                    SubTitle = ConfigService.Instance.GetConfigFilePath(),
                    IcoPath = IconHelper.GetIconPath("open"),
                    Action = _ =>
                    {
                        Helper.OpenInShell("notepad", ConfigService.Instance.GetConfigFilePath());
                        Context.API.ChangeQuery("", true);
                        return true;
                    },
                    Score = 900
                },
                new()
                {
                    Title = "Reload config",
                    SubTitle = "Reload the configuration file from disk",
                    IcoPath = IconHelper.GetIconPath("arrow-repeat-all"),
                    Action = _ =>
                    {
                        ConfigService.Instance.LoadConfig();
                        Context.API.ChangeQuery($"{query.ActionKeyword}", true);
                        return false;
                    },
                    Score = 700
                },
                new()
                {
                    Title = "Open Icon Folder",
                    SubTitle = "Store custom icons",
                    IcoPath = IconHelper.GetIconPath("icons"),
                    Action = _ =>
                    {
                        Helper.OpenInShell("explorer", IconHelper.GetIconFolderPath());
                        Context.API.ChangeQuery("", true);
                        return true;
                    },
                    Score = 600
                },
                new()
                {
                    Title = "View ProjectWalker GitHub repository",
                    IcoPath = IconHelper.GetIconPath("document-question"),
                    SubTitle = Context.CurrentPluginMetadata.Website,
                    Action = _ =>
                    {
                        Helper.OpenInShell(Context.CurrentPluginMetadata.Website);
                        Context.API.ChangeQuery("", true);
                        return true;
                    },
                    Score = 500
                }
            ];

            if (!string.IsNullOrWhiteSpace(ConfigService.Instance.Config.CustomEditorExecutablePath))
            {
                results.Add(new Result()
                {
                    Title = "Edit config in custom editor",
                    IcoPath = IconHelper.GetIconPath("open"),
                    SubTitle = $"{ConfigService.Instance.Config.CustomEditorExecutablePath} {ConfigService.Instance.GetConfigFilePath()}",
                    Action = _ =>
                    {
                        Helper.OpenInShell(ConfigService.Instance.Config.CustomEditorExecutablePath, ConfigService.Instance.GetConfigFilePath());
                        Context.API.ChangeQuery("", true);
                        return true;
                    },
                    Score = 800
                });
            }

            return results;
        }
        
        private List<Result> GenerateStandaloneRepoSearchResults(Query query)
        {
            var search = query.Search;

            var repos = Directory.GetDirectories(ConfigService.Instance.Config.BasePath);
            var folders = new List<string>();
            
            foreach (var repo in repos)
            {
                var repoName = new DirectoryInfo(repo).Name;
                if (ConfigService.Instance.Config.IgnoredFolders.Contains(repoName))
                {
                    continue;
                }
                
                folders.Add(repoName);
            }

            var folderResults = folders.Where(repoName => string.IsNullOrWhiteSpace(query.Search) || Fuzz.PartialRatio(repoName, search) > ConfigService.Instance.Config.SearchMatchRatio);
            return folderResults.Select(repoName => new Result()
            {
                QueryTextDisplay = search,
                Title = repoName,
                SubTitle = Path.Combine(ConfigService.Instance.Config.BasePath, repoName),
                IcoPath = IconHelper.GetIconPath("folder"),
                Action = _ =>
                {
                    Context.API.ChangeQuery($"{query.ActionKeyword} -o \"{repoName}\"", true);
                    return false;
                },
                ContextData = search,
            }).ToList();
        }

        private List<Result> GenerateProjectParentSearchResults(Query query)
        {
            var search = query.Search;

            var topLevels = Directory.GetDirectories(ConfigService.Instance.Config.BasePath);
            var folders = new List<Folder>();
            
            foreach (var topLevel in topLevels)
            {
                var topLevelName = new DirectoryInfo(topLevel).Name;
                if (ConfigService.Instance.Config.IgnoredFolders.Contains(topLevelName))
                {
                    continue;
                }
                
                var repoDirs = Directory.GetDirectories(topLevel);
                foreach (var repoDir in repoDirs)
                {
                    var repoName = new DirectoryInfo(repoDir).Name;
                    if (ConfigService.Instance.Config.IgnoredFolders.Contains(repoName))
                    {
                        continue;
                    }
                    
                    folders.Add(new Folder(topLevelName, repoName));
                }
            }

            var folderResults = folders.Where(x => string.IsNullOrWhiteSpace(query.Search) || Fuzz.PartialRatio($"{x.ProjectFolderName}/{x.RepoFolderName}", search) > ConfigService.Instance.Config.SearchMatchRatio);
            return folderResults.Select(x => new Result()
            {
                QueryTextDisplay = search,
                Title = x.RepoFolderName,
                SubTitle = x.ProjectFolderName,
                IcoPath = IconHelper.GetIconPath("folder"),
                Action = _ =>
                {
                    Context.API.ChangeQuery($"{query.ActionKeyword} -o \"{x.ProjectFolderName}\\{x.RepoFolderName}\"", true);
                    return false;
                },
                ContextData = search,
            }).ToList();
        }
        
        private List<Result> GenerateProjectOpenResults(Query query)
        {
            var regex = new Regex(Regex.Escape("-o"));
            var path = Path.Combine(ConfigService.Instance.Config.BasePath, regex.Replace(query.Search, string.Empty, 1).Replace("\"", string.Empty).Trim());

            if (!Path.Exists(path))
            {
                return
                [
                    new Result()
                    {
                        IcoPath = IconHelper.GetIconPath("error"),
                        Title = "Could not find path to folder",
                        SubTitle = path,
                        Action = _ =>
                        {
                            Clipboard.SetText(path);
                            Context.API.ChangeQuery("", true);
                            return true;
                        }
                    }
                ];
            }

            if (!ConfigService.Instance.Config.Options.Any())
            {
                return [GetErrorResult("No open options have been set")];
            }
            
            var results = new List<Result>();
            var builder = new OpenOptionBuilder();

            foreach (var option in ConfigService.Instance.Config.Options.OrderBy(x => x.Index))
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
            ConfigService.Instance.SetTheme(Context.API.GetCurrentTheme());
            ConfigService.Instance.LoadConfig();
        }

        private Result GetErrorResult(string message, string? subtitle = null)
        {
            return new Result()
            {
                IcoPath = IconHelper.GetIconPath("error"),
                Title = message,
                SubTitle = subtitle
            };
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
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
        
        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => ConfigService.Instance.SetTheme(newTheme);
        private record Folder(string ProjectFolderName, string RepoFolderName);
    }
}