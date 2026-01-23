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
                     Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    // File move
                    File.Move(sourcePath, destinationPath, overwrite);
                }
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Migration failed: {sourcePath} -> {destinationPath}", ex);
            return (false, ex.Message);
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
