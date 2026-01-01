using Xunit;
using LinkTo.Services;

namespace LinkTo.Tests.Services;

public class BatchLinkTests
{
    [Fact]
    public void GenerateBatchContent_WithWorkingDirectory_ShouldIncludeCdAndStart()
    {
        string sourcePath = @"C:\Apps\Test.exe";
        string workingDir = @"C:\Apps";
        
        string content = BatchLinkService.GenerateBatchContent(sourcePath, workingDir);
        
        Assert.Contains("@echo off", content);
        Assert.Contains(@"cd /d ""C:\Apps""", content);
        Assert.Contains(@"start """" ""C:\Apps\Test.exe""", content);
    }

    [Fact]
    public void GenerateBatchContent_WithoutWorkingDirectory_ShouldOnlyIncludeStart()
    {
        string sourcePath = @"C:\Apps\Test.exe";
        string workingDir = "";
        
        string content = BatchLinkService.GenerateBatchContent(sourcePath, workingDir);
        
        Assert.Contains("@echo off", content);
        Assert.DoesNotContain("cd /d", content);
        Assert.Contains(@"start """" ""C:\Apps\Test.exe""", content);
    }
}
