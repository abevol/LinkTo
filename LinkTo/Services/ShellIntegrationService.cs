using System;
using Microsoft.Win32;
using LinkTo.Helpers;

namespace LinkTo.Services;

/// <summary>
/// Service for Windows Shell context menu integration
/// </summary>
public class ShellIntegrationService
{
    private static readonly Lazy<ShellIntegrationService> _instance = new(() => new ShellIntegrationService());
    public static ShellIntegrationService Instance => _instance.Value;

    private const string FileShellKeyPath = @"*\shell\LinkTo";
    private const string DirectoryShellKeyPath = @"Directory\shell\LinkTo";
    private const string CommandSubKey = "command";
    private const string MenuText = "Link to...";
    private const string MenuTextChinese = "链接到...";

    private ShellIntegrationService() { }

    /// <summary>
    /// Check if context menu is currently registered
    /// </summary>
    public bool IsRegistered()
    {
        try
        {
            using var fileKey = Registry.ClassesRoot.OpenSubKey(FileShellKeyPath);
            using var dirKey = Registry.ClassesRoot.OpenSubKey(DirectoryShellKeyPath);
            return fileKey != null && dirKey != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Register context menu entries. Requires administrator privileges.
    /// </summary>
    public (bool Success, string? Error) Register()
    {
        if (!AdminHelper.IsRunningAsAdmin())
        {
            return (false, "Administrator privileges required");
        }

        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return (false, "Cannot determine executable path");
            }

            var command = $"\"{exePath}\" \"%1\"";
            var menuText = ConfigService.Instance.Language.StartsWith("zh") ? MenuTextChinese : MenuText;

            // Register for files
            RegisterMenuItem(FileShellKeyPath, menuText, command);
            
            // Register for directories
            RegisterMenuItem(DirectoryShellKeyPath, menuText, command);

            LogService.Instance.LogInfo("Shell context menu registered successfully");
            ConfigService.Instance.ShellMenuEnabled = true;
            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Failed to register shell context menu", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Unregister context menu entries. Requires administrator privileges.
    /// </summary>
    public (bool Success, string? Error) Unregister()
    {
        if (!AdminHelper.IsRunningAsAdmin())
        {
            return (false, "Administrator privileges required");
        }

        try
        {
            // Remove file menu
            Registry.ClassesRoot.DeleteSubKeyTree(FileShellKeyPath, false);
            
            // Remove directory menu
            Registry.ClassesRoot.DeleteSubKeyTree(DirectoryShellKeyPath, false);

            LogService.Instance.LogInfo("Shell context menu unregistered successfully");
            ConfigService.Instance.ShellMenuEnabled = false;
            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Failed to unregister shell context menu", ex);
            return (false, ex.Message);
        }
    }

    private static void RegisterMenuItem(string keyPath, string menuText, string command)
    {
        using var shellKey = Registry.ClassesRoot.CreateSubKey(keyPath, true);
        if (shellKey != null)
        {
            shellKey.SetValue(null, menuText);
            shellKey.SetValue("Icon", Environment.ProcessPath ?? string.Empty);

            using var commandKey = shellKey.CreateSubKey(CommandSubKey, true);
            commandKey?.SetValue(null, command);
        }
    }
}
