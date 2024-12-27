using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.Models;

public class PluginConfig
{
    public List<OpenOption> Options { get; set; } = [];
    public string BasePath { get; set; } = string.Empty;
}