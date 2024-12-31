using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;

public class OverrideConfig
{
    public string? Repo { get; set; }
    public string? Project { get; set; }
    public List<string> ExcludeOptions { get; set; } = [];
    public List<OpenOption> Options { get; set; } = [];
}