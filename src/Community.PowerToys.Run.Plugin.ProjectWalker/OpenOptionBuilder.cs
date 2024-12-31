using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;
using LibGit2Sharp;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker;

public class OpenOptionBuilder()
{
    public Result? BuildProcessResult(OpenOption option, Query query, string path)
    {
        if (string.IsNullOrWhiteSpace(option.ProcessName))
        {
            return null;
        }
        
        var flags = option.Parameters?.Trim();
        if (!string.IsNullOrWhiteSpace(flags))
        {
            flags = ReplaceVariables(flags, path);

            if (string.IsNullOrWhiteSpace(flags))
            {
                return null;
            }
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = $"{option.ProcessName} {flags}",
            Action = _ =>
            {
                Helper.OpenInShell(option.ProcessName, flags);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };

        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else if (ConfigService.Instance.Config.TryExtractProcessIcons)
        {
            var extractedIcon = IconHelper.GetProcessIconPath(option.ProcessName);
            result.IcoPath = !string.IsNullOrWhiteSpace(extractedIcon) ? extractedIcon : IconHelper.GetIconPath("open");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("open"); 
        }

        return result;
    }

    public Result? BuildBrowserResult(OpenOption option, Query query, string path)
    {
        var destination = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }
        
        destination = ReplaceVariables(destination, path);
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }

        if (!Uri.IsWellFormedUriString(destination, UriKind.Absolute))
        {
            return null;
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = destination,
            Action = _ =>
            {
                Helper.OpenInShell(destination);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
        
        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("globe");
        }

        return result;
    }

    public Result? BuildClipboardResult(OpenOption option, Query query, string path)
    {
        var text = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        text = ReplaceVariables(text, path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = text,
            Action = _ =>
            {
                Clipboard.SetText(text);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
        
        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("copy");
        }

        return result;
    }

    private string? ReplaceVariables(string original, string path)
    {
        var searchText = original;
        searchText = searchText.Replace("{{PATH}}", path);
        searchText = searchText.Replace("{{FOLDER}}", new DirectoryInfo(path).Name);

        searchText = ReplaceFileSearch(searchText, path);
        if (searchText == null)
        {
            return null;
        }

        searchText = ReplaceRecursiveFileSearch(searchText, path);
        if (searchText == null)
        {
            return null;
        }
        
        searchText = ReplaceGitVars(searchText, path);
        if (searchText == null)
        {
            return null;
        }

        return searchText;
    }

    private string? ReplaceFileSearch(string searchText, string path)
    {
        var fileExtMatches = Regex.Matches(searchText, "{{FILE:(.+)}}");
        if (!fileExtMatches.Any())
        {
            return searchText;
        }

        var ext = fileExtMatches.First().Groups[1];
        var fileResults = new DirectoryInfo(path).GetFiles($"{ext.Value}");

        if (fileResults.Any())
        {
            return searchText.Replace(fileExtMatches.First().Value, fileResults[0].FullName);
        }
            
        return null;
    }

    private string? ReplaceRecursiveFileSearch(string searchText, string path)
    {
        var fileExtMatches = Regex.Matches(searchText, "{{RECURSIVE_FILE:(.+)}}");
        if (!fileExtMatches.Any())
        {
            return searchText;
        }

        var ext = fileExtMatches.First().Groups[1];
        var fileResults = new DirectoryInfo(path).GetFiles($"{ext.Value}", SearchOption.AllDirectories);

        if (fileResults.Any())
        {
            return searchText.Replace(fileExtMatches.First().Value, fileResults[0].FullName);
        }
            
        return null;
    }

    private string? ReplaceGitVars(string searchText, string path)
    {
        var gitMatches = Regex.Matches(searchText, "{{GIT:(.+)}}");
        if (!gitMatches.Any())
        {
            return searchText;
        }

        if (!Repository.IsValid(path))
        {
            return null;
        }
        
        using var gitRepo = new Repository(path);
        foreach (var gitMatch in gitMatches.DistinctBy(x => x.Groups[1].Value))
        {
            var gitMatchKey = gitMatch.Groups[1].Value;
            if (gitMatchKey == "REMOTE_URL" && gitRepo.Network.Remotes.Any())
            {
                searchText = searchText.Replace(gitMatch.Value, gitRepo.Network.Remotes.First().Url);
            }
        }

        return searchText;
    }
}