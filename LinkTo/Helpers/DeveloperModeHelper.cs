using System;
using Microsoft.Win32;

namespace LinkTo.Helpers;

/// <summary>
/// Helper class for detecting Windows Developer Mode status
/// </summary>
public static class DeveloperModeHelper
{
    private const string DeveloperModeKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    private const string AllowDevelopmentWithoutDevLicense = "AllowDevelopmentWithoutDevLicense";

    /// <summary>
    /// Check if Windows Developer Mode is enabled
    /// Developer Mode allows creating symbolic links without admin privileges
    /// </summary>
    public static bool IsDeveloperModeEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(DeveloperModeKeyPath);
            if (key != null)
            {
                var value = key.GetValue(AllowDevelopmentWithoutDevLicense);
                if (value is int intValue)
                {
                    return intValue == 1;
                }
            }
        }
        catch
        {
            // Ignore registry access errors
        }

        return false;
    }
}
