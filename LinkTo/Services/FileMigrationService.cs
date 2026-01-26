using System;
using System.IO;
using System.Threading.Tasks;
using LinkTo.Helpers;
using Microsoft.VisualBasic.FileIO;

namespace LinkTo.Services;

/// <summary>
/// Service for handling file and directory migration operations with safety checks and Native Windows UI
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

            // Note: Microsoft.VisualBasic.FileIO handles most checks and UI dialogs automatically
            // but we still want to maintain the async nature and basic error handling.

            await Task.Run(() =>
            {
                if (Directory.Exists(sourcePath))
                {
                    FileSystem.MoveDirectory(
                        sourcePath, 
                        destinationPath, 
                        UIOption.AllDialogs, 
                        UICancelOption.ThrowException);
                }
                else
                {
                    FileSystem.MoveFile(
                        sourcePath, 
                        destinationPath, 
                        UIOption.AllDialogs, 
                        UICancelOption.ThrowException);
                }
            });

            return (true, null);
        }
        catch (OperationCanceledException)
        {
            LogService.Instance.LogInfo($"Migration cancelled by user: {sourcePath} -> {destinationPath}");
            return (false, "USER_CANCELLED");
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

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> RollbackAsync(string currentPath, string originalPath)
    {
        try
        {
            LogService.Instance.LogInfo($"Rolling back migration: {currentPath} -> {originalPath}");
            // Rollback usually doesn't need UI unless it's a large operation, 
            // but for consistency we use MoveAsync which now has UI.
            return await MoveAsync(currentPath, originalPath, overwrite: false);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Rollback failed: {currentPath} -> {originalPath}", ex);
            return (false, ex.Message);
        }
    }
}
