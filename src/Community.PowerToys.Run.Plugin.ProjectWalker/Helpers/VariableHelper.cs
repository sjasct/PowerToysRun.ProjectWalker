using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Helpers;

internal static class VariableHelper
{
    internal static string? ReplaceVariables(string original, string path)
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

    private static string? ReplaceFileSearch(string searchText, string path)
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

    private static string? ReplaceRecursiveFileSearch(string searchText, string path)
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

    private static string? ReplaceGitVars(string searchText, string path)
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