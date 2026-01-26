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
    /// Create a link (symbolic, hard, batch or shortcut) and add to history
    /// </summary>
    public (bool Success, string? Error) CreateLink(string sourcePath, string targetDirectory, string linkName, LinkType linkType, string? workingDir = null, bool migrateData = false)
    {
        if (migrateData)
        {
            return CreateLinkWithMigration(sourcePath, targetDirectory, linkName, linkType, workingDir);
        }

        var linkPath = Path.Combine(targetDirectory, linkName);
        var isDirectory = Directory.Exists(sourcePath);

        // Check if target exists
        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            return (false, GetLocalizedString("Error_TargetExists"));
        }

        // Create the link
        (bool Success, string? Error) result;
        
        switch (linkType)
        {
            case LinkType.Symbolic:
                result = CreateSymbolicLink(sourcePath, linkPath);
                break;
            case LinkType.Hard:
                result = CreateHardLink(sourcePath, linkPath);
                break;
            case LinkType.Batch:
                result = BatchLinkService.CreateBatchFile(sourcePath, linkPath, workingDir ?? string.Empty);
                break;
            case LinkType.Shortcut:
                result = ShortcutService.CreateShortcut(sourcePath, linkPath, workingDir ?? string.Empty);
                break;
            default:
                return (false, "Not implemented yet");
        }

        if (result.Success)
        {
            // Add to history
            var historyEntry = new LinkHistoryEntry
            {
                SourcePath = sourcePath,
                LinkPath = linkPath,
                WorkingDirectory = workingDir,
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

    private (bool Success, string? Error) CreateLinkWithMigration(string sourcePath, string targetDirectory, string linkName, LinkType linkType, string? workingDir)
    {
        // 1. Determine paths
        // In Migration Mode, "targetDirectory" is where the file moves TO.
        var destinationPath = Path.Combine(targetDirectory, linkName);
        
        // "Link" will be created at the ORIGINAL sourcePath.
        var linkLocation = sourcePath; 

        // 2. Validate Source
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
             return (false, GetLocalizedString("Error_SourceNotExist"));
        }

        // 3. Validate LinkType support before moving
        switch (linkType)
        {
            case LinkType.Symbolic:
            case LinkType.Hard:
            case LinkType.Batch:
            case LinkType.Shortcut:
                break;
            default:
                return (false, "Not implemented for this link type");
        }

        // 4. Move File/Folder
        // Blocking call for async method as CreateLink is synchronous
        var moveResult = System.Threading.Tasks.Task.Run(() => FileMigrationService.Instance.MoveAsync(sourcePath, destinationPath)).Result;
        
        if (!moveResult.Success)
        {
            // If the operation was cancelled or failed, we might need to rollback 
            // if it was a partial move (i.e., destination now exists).
            // Windows native cancellation is not always atomic for directories.
            if ((File.Exists(destinationPath) || Directory.Exists(destinationPath)) && 
                (File.Exists(sourcePath) || Directory.Exists(sourcePath)))
            {
                LogService.Instance.LogInfo("Migration failed or cancelled, but destination exists. Attempting rollback of partial move...");
                System.Threading.Tasks.Task.Run(() => FileMigrationService.Instance.RollbackAsync(destinationPath, sourcePath)).Wait();
            }
            
            return (false, moveResult.Error);
        }

        // 5. Create Link at original location (linkLocation) pointing to new location (destinationPath)
        (bool Success, string? Error) linkResult;
        
        switch (linkType)
        {
            case LinkType.Symbolic:
                linkResult = CreateSymbolicLink(destinationPath, linkLocation);
                break;
            case LinkType.Hard:
                linkResult = CreateHardLink(destinationPath, linkLocation);
                break;
            case LinkType.Batch:
                linkResult = BatchLinkService.CreateBatchFile(destinationPath, linkLocation, workingDir ?? string.Empty);
                break;
            case LinkType.Shortcut:
                linkResult = ShortcutService.CreateShortcut(destinationPath, linkLocation, workingDir ?? string.Empty);
                break;
            default:
                // Should not happen due to pre-check
                linkResult = (false, "Not implemented for this link type");
                break;
        }
        
        if (!linkResult.Success)
        {
            // 5. Rollback on Link Failure
            LogService.Instance.LogError($"Link creation failed after migration. Rolling back. Error: {linkResult.Error}");
            var rollbackResult = System.Threading.Tasks.Task.Run(() => FileMigrationService.Instance.RollbackAsync(destinationPath, sourcePath)).Result;
            
            if (!rollbackResult.Success)
            {
                 return (false, $"Link creation failed AND Rollback failed! Critical Error: {rollbackResult.Error}");
            }
            
            return (false, $"Link creation failed: {linkResult.Error} (Rolled back successfully)");
        }
        
        // 6. Success - History
        var isDirectory = Directory.Exists(destinationPath);
        
        var historyEntry = new LinkHistoryEntry
        {
            SourcePath = destinationPath, // Where data is now
            LinkPath = linkLocation,      // Where link is (original location)
            WorkingDirectory = workingDir,
            LinkType = linkType,
            IsDirectory = isDirectory,
            CreatedAt = DateTime.Now
        };
        ConfigService.Instance.AddLinkHistory(historyEntry);
        
        ConfigService.Instance.AddCommonDirectory(targetDirectory);

        return (true, null);
    }

    private static string GetLocalizedString(string key)
    {
        return LocalizationHelper.GetString(key);
    }
}
