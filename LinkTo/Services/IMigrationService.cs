using System.Threading.Tasks;

namespace LinkTo.Services;

/// <summary>
/// Interface for handling file and directory migration operations
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Moves a file or directory to a new location with rollback support.
    /// </summary>
    /// <param name="sourcePath">The full path of the source file or directory.</param>
    /// <param name="destinationPath">The full path of the destination.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    /// <returns>A task representing the asynchronous operation. Result is true if successful.</returns>
    Task<(bool Success, string? Error)> MoveAsync(string sourcePath, string destinationPath, bool overwrite = false);

    /// <summary>
    /// Moves a file or directory back to its original location (rollback).
    /// </summary>
    /// <param name="currentPath">The current location of the item.</param>
    /// <param name="originalPath">The original location to move back to.</param>
    /// <returns>A task representing the asynchronous operation. Result is true if successful.</returns>
    Task<(bool Success, string? Error)> RollbackAsync(string currentPath, string originalPath);
}
