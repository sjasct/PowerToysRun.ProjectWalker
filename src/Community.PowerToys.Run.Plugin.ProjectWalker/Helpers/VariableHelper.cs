using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using gfs.YamlDotNet.YamlPath;
using LibGit2Sharp;
using Wox.Plugin.Logger;
using YamlDotNet.RepresentationModel;

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
        try
        {
            var fileExtMatches = Regex.Matches(searchText, "{{(FILE|RECURSIVE_FILE):([^}]+?)(>[^}]*)?}}");
            if (!fileExtMatches.Any())
            {
                return searchText;
            }

            foreach (Match match in fileExtMatches)
            {
                var fileValue = match.Groups[2].Value;
                var fileResults = new DirectoryInfo(path).GetFiles($"{fileValue}", match.Groups[1].Value == "FILE" ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);

                if (fileResults.Any())
                {
                    var file = fileResults.First();
                    // a yml path has been specified, try get it
                    if (match.Groups.Values.Count(x => string.IsNullOrWhiteSpace(x.Value)) > 3)
                    {
                        var fileQuery = match.Groups[3].Value.TrimStart('>');
                        if ((file.Extension.ToLower() == ".yml" || file.Extension.ToLower() == ".yaml") && !YamlPathExtensions.GetQueryProblems(fileQuery).Any())
                        {
                            using var reader = new StreamReader(file.FullName);
                            var yml = new YamlStream();
                            yml.Load(reader);

                            var results = ((YamlMappingNode)yml.Documents[0].RootNode).Query(fileQuery).ToList();
                            if (results.Any())
                            {
                                searchText = searchText.Replace(match.Value, results.First().ToString());
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        searchText = searchText.Replace(match.Value, file.FullName);
                    }
                }
                else
                {
                    return null;
                }
            }

            return searchText;
        }
        catch (Exception ex)
        {
            Log.Exception($"Exception thrown trying to replace string '{searchText}'", ex, typeof(VariableHelper));
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