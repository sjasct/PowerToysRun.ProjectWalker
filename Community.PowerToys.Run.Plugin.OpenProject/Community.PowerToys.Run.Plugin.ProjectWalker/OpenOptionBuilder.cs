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
            flags = Replace(flags, path);

            if (string.IsNullOrWhiteSpace(flags))
            {
                return null;
            }
        }
        
        return new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            IcoPath = ConfigHelper.Instance.GetProcessIconPath(option.ProcessName),
            SubTitle = $"{option.ProcessName} {flags}",
            Action = _ =>
            {
                Helper.OpenInShell(option.ProcessName, flags);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigHelper.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
    }

    public Result? BuildBrowserResult(OpenOption option, Query query, string path)
    {
        var destination = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }
        
        destination = Replace(destination, path);
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }

        if (!Uri.IsWellFormedUriString(destination, UriKind.Absolute))
        {
            return null;
        }
        
        return new Result()
        {
            QueryTextDisplay = query.Search,
            IcoPath = ConfigHelper.Instance.GetBaseIconPath(),
            Title = option.Name,
            SubTitle = destination,
            Action = _ =>
            {
                Helper.OpenInShell(destination);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigHelper.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
    }

    public Result? BuildClipboardResult(OpenOption option, Query query, string path)
    {
        var text = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        text = Replace(text, path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        return new Result()
        {
            QueryTextDisplay = query.Search,
            IcoPath = ConfigHelper.Instance.GetBaseIconPath(),
            Title = option.Name,
            SubTitle = text,
            Action = _ =>
            {
                Clipboard.SetText(text);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigHelper.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
    }

    private string? Replace(string original, string path)
    {
        var searchText = original;
        searchText = searchText.Replace("{{PATH}}", path);

        searchText = ReplaceFileSearch(searchText, path);
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