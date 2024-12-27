namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;

public class OpenOption
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required int Index { get; set; }
    public string? ProcessName { get; set; }
    public string? Parameters { get; set; }
}