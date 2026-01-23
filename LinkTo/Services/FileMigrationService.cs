using System;
using System.IO;
using System.Threading.Tasks;
using LinkTo.Helpers;

namespace LinkTo.Services;

/// <summary>
/// Service for handling file and directory migration operations with safety checks
/// </summary>
public class FileMigrationService : IMigrationService
{
    private static readonly Lazy<FileMigrationService> _instance = new(() => new FileMigrationService());
    public static FileMigrationService Instance => _instance.Value;

    private FileMigrationService() { }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> MoveAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        try
        {
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                return (false, "Source does not exist");
            }

            // Explicit check for destination existence to provide consistent error message
            if ((File.Exists(destinationPath) || Directory.Exists(destinationPath)) && !overwrite)
            {
                return (false, "Target already exists");
            }

            await Task.Run(() =>
            {
                // Ensure parent directory of destination exists
                var parentDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                if (Directory.Exists(sourcePath))
                {
                     // Directory move
                     // Note: Conflict handling will be refined in a later task
                     if (overwrite && Directory.Exists(destinationPath))
                     {
                         // Basic overwrite for directory: Delete dest then move
                         Directory.Delete(destinationPath, true);
                     }

                     // Check for cross-volume move
                     var sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourcePath));
                     var destRoot = Path.GetPathRoot(Path.GetFullPath(destinationPath));

                     if (!string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase))
                     {
                         // Directory.Move does not support cross-volume, so we must Copy + Delete
                         CopyDirectory(sourcePath, destinationPath);
                         Directory.Delete(sourcePath, true);
                     }
                     else
                     {
                         Directory.Move(sourcePath, destinationPath);
                     }
                }
                else
                {
                    // File move (File.Move supports cross-volume)
                    File.Move(sourcePath, destinationPath, overwrite);
                }
            });

            return (true, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogService.Instance.LogError($"Migration failed (Access Denied): {sourcePath} -> {destinationPath}", ex);
            return (false, "ERROR_UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Migration failed: {sourcePath} -> {destinationPath}", ex);
            return (false, ex.Message);
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        // Ensure destination directory exists
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // Copy subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> RollbackAsync(string currentPath, string originalPath)
    {
        try
        {
            LogService.Instance.LogInfo($"Rolling back migration: {currentPath} -> {originalPath}");
            return await MoveAsync(currentPath, originalPath, overwrite: false);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Rollback failed: {currentPath} -> {originalPath}", ex);
            return (false, ex.Message);
        }
    }
}
