using Xunit;
using LinkTo.Services;

namespace LinkTo.Tests.Services;

public class BatchLinkTests
{
    [Fact]
    public void GenerateBatchContent_ShouldIncludePathAndArgs()
    {
        string sourcePath = @"C:\Apps\Test.exe";
        string workingDir = @"C:\Apps";
        
        string content = BatchLinkService.GenerateBatchContent(sourcePath, workingDir);
        
        Assert.Contains("@echo off", content);
        Assert.Contains(@"""C:\Apps\Test.exe"" %*", content);
        Assert.DoesNotContain("cd /d", content);
        Assert.DoesNotContain("start", content);
    }

    [Fact]
    public void GenerateBatchContent_WithoutWorkingDirectory_ShouldStillIncludePathAndArgs()
    {
        string sourcePath = @"C:\Apps\Test.exe";
        string workingDir = "";
        
        string content = BatchLinkService.GenerateBatchContent(sourcePath, workingDir);
        
        Assert.Contains("@echo off", content);
        Assert.Contains(@"""C:\Apps\Test.exe"" %*", content);
        Assert.DoesNotContain("start", content);
    }
}
