namespace Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.Models;

public class OpenOption
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required int Index { get; set; }
    public string? ProcessName { get; set; }
    public string? Arguments { get; set; }
}