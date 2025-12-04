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

    /// <summary>
    /// Restart the application with administrator privileges
    /// </summary>
    /// <returns>True if restart was initiated successfully</returns>
    public static bool RestartAsAdmin(string? arguments = null)
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = arguments ?? string.Empty
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
