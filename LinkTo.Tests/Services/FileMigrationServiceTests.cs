using Xunit;
using LinkTo.Services;
using System.IO;
using System;
using System.Threading.Tasks;

namespace LinkTo.Tests.Services;

public class FileMigrationServiceTests : IDisposable
{
    private readonly string _tempDir;

    public FileMigrationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LinkToTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task MoveAsync_ShouldMoveFile_WhenSourceIsFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "source.txt");
        var destFile = Path.Combine(_tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");

        // Act
        var result = await FileMigrationService.Instance.MoveAsync(sourceFile, destFile);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.False(File.Exists(sourceFile), "Source file should be gone");
        Assert.True(File.Exists(destFile), "Destination file should exist");
        Assert.Equal("test content", await File.ReadAllTextAsync(destFile));
    }

    [Fact]
    public async Task MoveAsync_ShouldMoveDirectory_WhenSourceIsDirectory()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "SourceDir");
        var destDir = Path.Combine(_tempDir, "DestDir");
        Directory.CreateDirectory(sourceDir);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "file.txt"), "content");

        // Act
        var result = await FileMigrationService.Instance.MoveAsync(sourceDir, destDir);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.False(Directory.Exists(sourceDir), "Source dir should be gone");
        Assert.True(Directory.Exists(destDir), "Destination dir should exist");
        Assert.True(File.Exists(Path.Combine(destDir, "file.txt")));
    }

    [Fact]
    public async Task MoveAsync_ShouldFail_WhenSourceDoesNotExist()
    {
        // Arrange
        var sourceFile = Path.Combine(_tempDir, "nonexistent.txt");
        var destFile = Path.Combine(_tempDir, "dest.txt");

        // Act
        var result = await FileMigrationService.Instance.MoveAsync(sourceFile, destFile);

        // Assert
        Assert.False(result.Success);
        // We expect some error message about not existing
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public async Task RollbackAsync_ShouldMoveBack_WhenCalled()
    {
        // Arrange
        var originalPath = Path.Combine(_tempDir, "original.txt");
        var currentPath = Path.Combine(_tempDir, "moved.txt");
        await File.WriteAllTextAsync(currentPath, "content");
        
        // Ensure original path is empty
        if (File.Exists(originalPath)) File.Delete(originalPath);

        // Act
        var result = await FileMigrationService.Instance.RollbackAsync(currentPath, originalPath);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.False(File.Exists(currentPath), "Current file should be gone");
        Assert.True(File.Exists(originalPath), "File should be back at original path");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { /* Ignore cleanup errors */ }
    }
}
