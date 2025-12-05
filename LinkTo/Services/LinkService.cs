using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Helpers;
using LinkTo.Models;

namespace LinkTo.Services;

/// <summary>
/// Service for creating and managing symbolic and hard links
/// </summary>
public partial class LinkService
{
    private static readonly Lazy<LinkService> _instance = new(() => new LinkService());
    public static LinkService Instance => _instance.Value;

    // P/Invoke for CreateHardLink
    [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkNative(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    private LinkService() { }

    /// <summary>
    /// Check if the source can use hard link
    /// Hard links only work for files (not directories), on the same volume, and not for network paths
    /// </summary>
    public (bool CanUse, string? Reason) CanCreateHardLink(string sourcePath, string targetDirectory)
    {
        // Check if source exists
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            return (false, GetLocalizedString("HardLinkReason_SourceNotExist"));
        }

        // Hard links don't support directories
        if (Directory.Exists(sourcePath))
        {
            return (false, GetLocalizedString("HardLinkReason_DirectoryNotSupported"));
        }

        // Check network path
        if (sourcePath.StartsWith(@"\\") || targetDirectory.StartsWith(@"\\"))
        {
            return (false, GetLocalizedString("HardLinkReason_NetworkNotSupported"));
        }

        // Check same volume
        var sourceRoot = Path.GetPathRoot(sourcePath);
        var targetRoot = Path.GetPathRoot(targetDirectory);
        if (!string.Equals(sourceRoot, targetRoot, StringComparison.OrdinalIgnoreCase))
        {
            return (false, GetLocalizedString("HardLinkReason_CrossVolumeNotSupported"));
        }

        return (true, null);
    }

    /// <summary>
    /// Check if symbolic link creation requires elevation
    /// </summary>
    public bool RequiresElevationForSymbolicLink()
    {
        return !DeveloperModeHelper.IsDeveloperModeEnabled() && !AdminHelper.IsRunningAsAdmin();
    }

    /// <summary>
    /// Create a symbolic link
    /// </summary>
    public (bool Success, string? Error) CreateSymbolicLink(string sourcePath, string linkPath)
    {
        try
        {
            LogService.Instance.LogInfo($"Creating symbolic link: {linkPath} -> {sourcePath}");

            if (Directory.Exists(sourcePath))
            {
                Directory.CreateSymbolicLink(linkPath, sourcePath);
            }
            else if (File.Exists(sourcePath))
            {
                File.CreateSymbolicLink(linkPath, sourcePath);
            }
            else
            {
                return (false, GetLocalizedString("Error_SourceNotExist"));
            }

            LogService.Instance.LogInfo("Symbolic link created successfully");
            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            LogService.Instance.LogError("Symbolic link creation failed: Access denied");
            return (false, GetLocalizedString("Error_AccessDenied"));
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Symbolic link creation failed", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Create a hard link
    /// </summary>
    public (bool Success, string? Error) CreateHardLink(string sourcePath, string linkPath)
    {
        try
        {
            LogService.Instance.LogInfo($"Creating hard link: {linkPath} -> {sourcePath}");

            if (!File.Exists(sourcePath))
            {
                return (false, GetLocalizedString("Error_SourceNotExist"));
            }

            if (!CreateHardLinkNative(linkPath, sourcePath, IntPtr.Zero))
            {
                var errorCode = Marshal.GetLastWin32Error();
                LogService.Instance.LogError($"Hard link creation failed with error code: {errorCode}");
                return (false, $"{GetLocalizedString("Error_HardLinkFailed")} (0x{errorCode:X})");
            }

            LogService.Instance.LogInfo("Hard link created successfully");
            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Hard link creation failed", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Delete a link (file or directory)
    /// </summary>
    public (bool Success, string? Error) DeleteLink(string linkPath)
    {
        try
        {
            LogService.Instance.LogInfo($"Deleting link: {linkPath}");

            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath, false);
            }
            else if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
            }
            else
            {
                // Link doesn't exist, consider it deleted
                return (true, null);
            }

            LogService.Instance.LogInfo("Link deleted successfully");
            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Link deletion failed", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Create a link (symbolic or hard) and add to history
    /// </summary>
    public (bool Success, string? Error) CreateLink(string sourcePath, string targetDirectory, string linkName, LinkType linkType)
    {
        var linkPath = Path.Combine(targetDirectory, linkName);
        var isDirectory = Directory.Exists(sourcePath);

        // Check if target exists
        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            return (false, GetLocalizedString("Error_TargetExists"));
        }

        // Create the link
        var result = linkType == LinkType.Symbolic
            ? CreateSymbolicLink(sourcePath, linkPath)
            : CreateHardLink(sourcePath, linkPath);

        if (result.Success)
        {
            // Add to history
            var historyEntry = new LinkHistoryEntry
            {
                SourcePath = sourcePath,
                LinkPath = linkPath,
                LinkType = linkType,
                IsDirectory = isDirectory,
                CreatedAt = DateTime.Now
            };
            ConfigService.Instance.AddLinkHistory(historyEntry);

            // Add target directory to common directories
            ConfigService.Instance.AddCommonDirectory(targetDirectory);
        }

        return result;
    }

    private static string GetLocalizedString(string key)
    {
        try
        {
            var resourceLoader = new ResourceLoader();
            var value = resourceLoader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            // Fallback to key if resource loading fails
            return key;
        }
    }
}
