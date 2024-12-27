using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;
using ManagedCommon;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker;

public class ConfigHelper
{
    public static ConfigHelper Instance { get; } = new ConfigHelper();

    private Theme _theme = Theme.System;

    public PluginConfig Config { get; private set; } = GetDefaultConfiguration();
    
    public void SetTheme(Theme theme)
    {
        _theme = theme;
    }

    public void LoadConfig()
    {
        var configFile = GetConfigFilePath();
        if (!File.Exists(configFile))
        {
            File.WriteAllText(configFile, JsonSerializer.Serialize(Config, JsonSerializerOptions.Web));
            return;
        }

        var fullConfig =
            JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(configFile), JsonSerializerOptions.Web);

        if (fullConfig == null)
        {
            Logger.LogError("Failed to deserialize config");
            throw new Exception("Failed to load config");
        }

        Config = fullConfig;
    }
    
    public string GetProcessIconPath(string processName)
    {
        var fileName = Path.GetFileNameWithoutExtension(processName);
        
        var iconCachePath = GetAndCreateBaseSettingsPath("IconCache");
        var cacheIconFilePath = Path.Combine(iconCachePath, $"{fileName}.png");
        if (File.Exists(cacheIconFilePath))
        {
            return cacheIconFilePath;
        }

        try
        {
            var extractedIcon = Icon.ExtractAssociatedIcon(processName);
            if (extractedIcon != null)
            {
                using var fs = new FileStream(cacheIconFilePath, FileMode.Create);
                extractedIcon.Save(fs);
                return cacheIconFilePath;
            }
        }
        catch (ArgumentException)
        {
        }
        catch (FileNotFoundException)
        {
        }
        
        return GetBaseIconPath();
    }

    public string GetBaseIconPath()
    {
        return _theme == Theme.Light || _theme == Theme.HighContrastWhite
        ? "Images/powertoysrun.projectwalker.light.png"
        : "Images/powertoysrun.projectwalker.dark.png";
    }
    
    public string GetConfigFilePath()
    {
        return Path.Combine(GetAndCreateBaseSettingsPath(), "config.json");
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
    
    private static string GetAndCreateBaseSettingsPath(string? subfolder = null)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectWalker", subfolder ?? string.Empty);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}