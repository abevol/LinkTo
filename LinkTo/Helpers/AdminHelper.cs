using System;
using System.Security.Principal;
using System.Diagnostics;

namespace LinkTo.Helpers;

/// <summary>
/// Helper class for administrator privilege detection and elevation
/// </summary>
public static class AdminHelper
{
    /// <summary>
    /// Check if the current process is running with administrator privileges
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string? GetExecutablePath()
    {
        return Environment.ProcessPath;
    }

    /// <summary>
    /// Restart the application with administrator privileges
    /// </summary>
    /// <returns>True if restart was initiated successfully</returns>
    public static bool RestartAsAdmin(params string[] arguments)
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restart the application with the current privileges
    /// </summary>
    /// <returns>True if restart was initiated successfully</returns>
    public static bool Restart()
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
