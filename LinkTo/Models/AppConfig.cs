using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LinkTo.Models;

/// <summary>
/// Link type enumeration
/// </summary>
public enum LinkType
{
    Symbolic,
    Hard,
    Batch,
    Shortcut
}

/// <summary>
/// Represents a single link history entry
/// </summary>
public class LinkHistoryEntry
{
    public LinkHistoryEntry() { }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourcePath { get; set; } = string.Empty;
    public string LinkPath { get; set; } = string.Empty;
    public string? WorkingDirectory { get; set; }
    public LinkType LinkType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDirectory { get; set; }
}

/// <summary>
/// Application configuration model
/// </summary>
public class AppConfig
{
    public AppConfig() { }

    public string Language { get; set; } = "en-US";
    public bool ShellMenuEnabled { get; set; } = false;
    public List<string> CommonDirectories { get; set; } = new();
    public List<LinkHistoryEntry> LinkHistory { get; set; } = new();
}

/// <summary>
/// JSON source generation context for AOT compatibility
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(LinkHistoryEntry))]
[JsonSerializable(typeof(CommonDirItem))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<LinkHistoryEntry>))]
[JsonSerializable(typeof(List<CommonDirItem>))]
[JsonSerializable(typeof(LinkType))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
public partial class AppConfigJsonContext : JsonSerializerContext
{
}
