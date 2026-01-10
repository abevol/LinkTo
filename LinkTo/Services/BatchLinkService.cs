using System;
using System.IO;
using System.Text;

namespace LinkTo.Services;

/// <summary>
/// Service for creating Batch file launchers
/// </summary>
public static class BatchLinkService
{
    /// <summary>
    /// Generates the content for a batch file that launches the source with an optional working directory
    /// </summary>
    public static string GenerateBatchContent(string sourcePath, string workingDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine($@"""{sourcePath}"" %*");
        
        return sb.ToString();
    }

    /// <summary>
    /// Creates a batch file that launches the source
    /// </summary>
    public static (bool Success, string? Error) CreateBatchFile(string sourcePath, string targetPath, string workingDir)
    {
        try
        {
            LogService.Instance.LogInfo($"Creating batch link: {targetPath} -> {sourcePath} (WorkDir: {workingDir})");
            string content = GenerateBatchContent(sourcePath, workingDir);
            File.WriteAllText(targetPath, content, Encoding.Default);
            LogService.Instance.LogInfo("Batch link created successfully");
            return (true, null);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Batch link creation failed", ex);
            return (false, ex.Message);
        }
    }
}
