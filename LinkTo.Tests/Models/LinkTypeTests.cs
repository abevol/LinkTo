using Xunit;
using LinkTo.Models;
using System;

namespace LinkTo.Tests.Models;

public class LinkTypeTests
{
    [Fact]
    public void LinkType_ShouldHaveBatchAndShortcut()
    {
        // This test verifies that the LinkType enum has the expected new values.
        // It relies on parsing string to enum, which will fail if the value doesn't exist 
        // OR we can just try to cast integer if we knew the value, but string parsing is safer check for existence by name.
        
        var hasBatch = Enum.TryParse<LinkType>("Batch", out _);
        var hasShortcut = Enum.TryParse<LinkType>("Shortcut", out _);

        Assert.True(hasBatch, "LinkType should contain 'Batch'");
        Assert.True(hasShortcut, "LinkType should contain 'Shortcut'");
    }
}
