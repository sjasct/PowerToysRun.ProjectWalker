using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Wox.Plugin.Logger;

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

        searchText = ReplaceGitVars(searchText, path);
        if (searchText == null)
        {
            return null;
        }

        return searchText;
    }

    private static string? ReplaceFileSearch(string searchText, string path)
    {
        var fileExtMatches = Regex.Matches(searchText, "{{(FILE|RECURSIVE_FILE):(.+)}}");
        if (!fileExtMatches.Any())
        {
            return searchText;
        }

        foreach (Match match in fileExtMatches)
        {
            var searchValue = match.Groups[2];
            var fileResults = new DirectoryInfo(path).GetFiles($"{searchValue.Value}", match.Groups[1].Value == "FILE" ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);

            if (fileResults.Any())
            {
                searchText = searchText.Replace(match.Value, fileResults[0].FullName);
            }
            else
            {
                return null;
            }
        }

        return searchText;
    }

    private static string? ReplaceGitVars(string searchText, string path)
    {
        var gitMatches = Regex.Matches(searchText, "{{GIT:(.+)}}");
        if (!gitMatches.Any())
        {
            return searchText;
        }

        try
        {
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
        }
        catch (LibGit2SharpException ex)
        {
            Log.Exception("Exception thrown trying to replace Git related variables", ex, typeof(VariableHelper));
            return null;
        }
        
        return searchText;
    }
}