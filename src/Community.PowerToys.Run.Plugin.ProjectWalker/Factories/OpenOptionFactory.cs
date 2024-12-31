using System;
using System.Linq;
using System.Windows;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Helpers;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Models;
using Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Services;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.ProjectWalker.Factories;

internal static class OpenOptionFactory
{
    internal static Result? CreateOption(OpenOption option, Query query, string path)
    {
        return option.Type switch
        {
            "process" => BuildProcessResult(option, query, path),
            "browser" => BuildBrowserResult(option, query, path),
            "clipboard" => BuildClipboardResult(option, query, path),
            _ => null
        };
    }
    
    private static Result? BuildProcessResult(OpenOption option, Query query, string path)
    {
        if (string.IsNullOrWhiteSpace(option.ProcessName))
        {
            return null;
        }
        
        var flags = option.Parameters?.Trim();
        if (!string.IsNullOrWhiteSpace(flags))
        {
            flags = VariableHelper.ReplaceVariables(flags, path);
            if (string.IsNullOrWhiteSpace(flags))
            {
                return null;
            }
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = $"{option.ProcessName} {flags}",
            Action = _ =>
            {
                Helper.OpenInShell(option.ProcessName, flags);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };

        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else if (ConfigService.Instance.Config.TryExtractProcessIcons)
        {
            var extractedIcon = IconHelper.GetProcessIconPath(option.ProcessName);
            result.IcoPath = !string.IsNullOrWhiteSpace(extractedIcon) ? extractedIcon : IconHelper.GetIconPath("open");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("open"); 
        }

        return result;
    }

    private static Result? BuildBrowserResult(OpenOption option, Query query, string path)
    {
        var destination = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }
        
        destination = VariableHelper.ReplaceVariables(destination, path);
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }

        if (!Uri.IsWellFormedUriString(destination, UriKind.Absolute))
        {
            return null;
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = destination,
            Action = _ =>
            {
                Helper.OpenInShell(destination);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
        
        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("globe");
        }

        return result;
    }

    private static Result? BuildClipboardResult(OpenOption option, Query query, string path)
    {
        var text = option.Parameters?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        text = VariableHelper.ReplaceVariables(text, path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        
        var result = new Result()
        {
            QueryTextDisplay = query.Search,
            Title = option.Name,
            SubTitle = text,
            Action = _ =>
            {
                Clipboard.SetText(text);
                return true;
            },
            ContextData = query.Search,
            Score = ConfigService.Instance.Config.Options.Max(x => x.Index) - option.Index
        };
        
        if (!string.IsNullOrWhiteSpace(option.IconPath) && IconHelper.TryGetCustomIcon(option.IconPath, out string? customIconPath))
        {
            result.IcoPath = customIconPath ?? throw new ArgumentNullException(nameof(customIconPath), "TryGetCustomIcon returned true but outputted null path");
        }
        else
        {
            result.IcoPath = IconHelper.GetIconPath("copy");
        }

        return result;
    }
}