using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;

public class PluginConfig
{
    public string BasePath { get; set; } = string.Empty;
    public string? CustomEditorExecutablePath { get; set; }
    public FolderStructureType FolderStructureType { get; set; } = FolderStructureType.StandaloneRepos;
    public int SearchMatchRatio { get; set; } = 70;
    public List<string> IgnoredFolders { get; set; } = []; 
    public List<OpenOption> Options { get; set; } = [];
    public List<OverrideConfig> Overrides { get; set; } = [];
    public bool TryExtractProcessIcons { get; set; } = false;
}