using Xunit;
using LinkTo.Services;
using System.IO;
using System;

namespace LinkTo.Tests.Services;

public class ShortcutServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _tempShortcut;

    public ShortcutServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        _tempShortcut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".lnk");
        File.WriteAllText(_tempFile, "test");
    }

    [Fact]
    public void CreateShortcut_ShouldCreateLnkFile()
    {
        string workingDir = Path.GetTempPath();
        
        var (success, error) = ShortcutService.CreateShortcut(_tempFile, _tempShortcut, workingDir);
        
        Assert.True(success, error);
        Assert.True(File.Exists(_tempShortcut));
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (File.Exists(_tempShortcut)) File.Delete(_tempShortcut);
    }
}
