using System;
using System.IO;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;
using ManagedCommon;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Services;

internal class ConfigService
{
    internal static ConfigService Instance { get; } = new ConfigService();

    internal Theme Theme { get; private set; }

    internal PluginConfig Config { get; private set; } = GetDefaultConfiguration();
    
    internal void SetTheme(Theme theme)
    {
        Theme = theme;
    }

    internal void LoadConfig()
    {
        var configFile = GetConfigFilePath();
        if (!File.Exists(configFile))
        {
            File.WriteAllText(configFile, JsonSerializer.Serialize(Config, JsonSerializerOptions.Web));
            return;
        }

        var fullConfig = JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(configFile), JsonSerializerOptions.Web);

        if (fullConfig == null)
        {
            Log.Error("Failed to deserialize config", typeof(ConfigService));
            throw new Exception("Failed to load config");
        }

        Config = fullConfig;
    }
    
    internal string GetConfigFilePath()
    {
        return Path.Combine(GetAndCreateBaseSettingsPath(), "config.json");
    }
    
    internal static string GetAndCreateBaseSettingsPath(string? subfolder = null)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectWalker", subfolder ?? string.Empty);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
    
    private static PluginConfig GetDefaultConfiguration()
    {
        return new PluginConfig()
        {
            Options =
            [
                new OpenOption()
                {
                    Type = "process",
                    Name = "Explorer",
                    Index = 0,
                    ProcessName = "explorer",
                    Parameters = "{{PATH}}"
                },
                new OpenOption()
                {
                    Type = "process",
                    Name = "VS Code",
                    Index = 1,
                    ProcessName = "code",
                    Parameters = "{{PATH}}"
                },
                new OpenOption()
                {
                    Type = "browser",
                    Name = "Open in Browser",
                    Index = 2,
                    Parameters = "{{GIT:REMOTE_URL}}"
                },
                new OpenOption()
                {
                    Type = "clipboard",
                    Name = "Copy path",
                    Index = 3,
                    Parameters = "{{PATH}}"
                },
            ]
        };
    }
}