using Xunit;
using LinkTo.Services;
using LinkTo.Models;
using System.IO;
using System;

namespace LinkTo.Tests.Services;

public class LinkServiceTests : IDisposable
{
    private readonly string _tempDir;

    public LinkServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LinkServiceTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void CreateLink_ShouldMigrateAndCreateLink_WhenMigrateDataIsTrue()
    {
        // Arrange
        var sourceContent = "test content";
        var sourceFileName = "source.txt";
        var sourcePath = Path.Combine(_tempDir, sourceFileName);
        var targetDir = Path.Combine(_tempDir, "Target");
        Directory.CreateDirectory(targetDir);
        
        File.WriteAllText(sourcePath, sourceContent);

        // Act
        // Pass migrateData: true
        var result = LinkService.Instance.CreateLink(sourcePath, targetDir, sourceFileName, LinkType.Symbolic, null, migrateData: true);

        // Assert
        Assert.True(result.Success, result.Error);
        
        var expectedRealFilePath = Path.Combine(targetDir, sourceFileName);
        var expectedLinkLocation = sourcePath; // Original location

        Assert.True(File.Exists(expectedRealFilePath), "Real file should be at target directory");
        Assert.Equal(sourceContent, File.ReadAllText(expectedRealFilePath));
        
        // Verify Link
        var linkInfo = new FileInfo(expectedLinkLocation);
        Assert.True(linkInfo.Exists, "Link should exist at original location");
        Assert.True((linkInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint, "Should be a reparse point (link)");
    }

    [Fact]
    public void CreateLink_ShouldCreateNormalLink_WhenMigrateDataIsFalse()
    {
        // Arrange
        var sourceContent = "source content";
        var sourceFileName = "source_normal.txt";
        var sourcePath = Path.Combine(_tempDir, sourceFileName);
        var targetDir = Path.Combine(_tempDir, "TargetNormal");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(sourcePath, sourceContent);
        
        var linkName = "link.txt";

        // Act
        var result = LinkService.Instance.CreateLink(sourcePath, targetDir, linkName, LinkType.Symbolic); // Default false

        // Assert
        Assert.True(result.Success, result.Error);
        
        var expectedLinkPath = Path.Combine(targetDir, linkName);
        
        Assert.True(File.Exists(sourcePath), "Source should stay");
        Assert.True(File.Exists(expectedLinkPath), "Link should exist");
        
        var linkInfo = new FileInfo(expectedLinkPath);
        Assert.True((linkInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch {}
    }
}
