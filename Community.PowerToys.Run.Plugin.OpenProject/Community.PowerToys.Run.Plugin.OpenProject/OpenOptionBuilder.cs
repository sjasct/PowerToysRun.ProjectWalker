using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.Models;
using LibGit2Sharp;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject;

public class OpenOptionBuilder(string coreIconPath, PluginConfig config)
{
    public Result? BuildProcessResult(OpenOption option, Query query, string path)
    {
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
                    return null;
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
        
        return new Result()
        {
            QueryTextDisplay = query.Search,
            IcoPath = coreIconPath,
            Title = option.Name,
            SubTitle = $"{option.ProcessName} {flags}",
            Action = _ =>
            {
                Helper.OpenInShell(option.ProcessName, flags);
                return true;
            },
            ContextData = query.Search,
            Score = config.Options.Max(x => x.Index) - option.Index
        };
    }

    public Result? BuildBrowserResult(OpenOption option, Query query, string path)
    {
        return null;
    }

    public Result? BuildClipboardResult(OpenOption option, Query query, string path)
    {
        return null;
    }
}