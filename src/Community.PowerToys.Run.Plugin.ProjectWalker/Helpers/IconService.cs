using System;
using System.Drawing;
using System.IO;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Services;
using ManagedCommon;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Helpers;

internal static class IconHelper
{
    internal static string? GetProcessIconPath(string processName)
    {
        var fileName = Path.GetFileNameWithoutExtension(processName);
        
        var iconCachePath = ConfigService.GetAndCreateBaseSettingsPath(Path.Combine("icons", "cache"));
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
        catch (ArgumentException ex)
        {
            Log.Exception("Ignored ArgumentException", ex, typeof(IconHelper));
        }
        catch (FileNotFoundException ex)
        {
            Log.Exception("Ignored FileNotFoundException", ex, typeof(IconHelper));
        }

        return null;
    }
    
    internal static bool TryGetCustomIcon(string input, out string? path)
    {
        if (Path.IsPathRooted(input) && File.Exists(input))
        {
            path = input;
            return true;
        }

        if (Path.Exists(Path.Combine(GetIconFolderPath(), input)))
        {
            path = Path.Combine(GetIconFolderPath(), input);
            return true;
        }

        if (Path.Exists(Path.Combine(GetIconFolderPath(), $"{input}.png")))
        {
            path = Path.Combine(GetIconFolderPath(), $"{input}.png");
            return true;
        }

        path = null;
        return false;
    }
    
    internal static string GetIconPath(string iconName)
    {
        return ConfigService.Instance.Theme == Theme.Light || ConfigService.Instance.Theme == Theme.HighContrastWhite ? $"Images/{iconName}.light.png" : $"Images/{iconName}.dark.png";
    }
    
    internal static string GetIconFolderPath()
    {
        return ConfigService.GetAndCreateBaseSettingsPath("icons");
    }
}