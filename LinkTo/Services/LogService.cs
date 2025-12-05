using System;
using System.IO;

namespace LinkTo.Services;

/// <summary>
/// Simple file logging service
/// </summary>
public class LogService
{
    private static readonly Lazy<LogService> _instance = new(() => new LogService());
    public static LogService Instance => _instance.Value;

    private readonly string _logDirectory;
    private readonly object _lock = new();

    private LogService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LinkTo",
            "Logs");

        Directory.CreateDirectory(_logDirectory);
    }

    private string GetLogFilePath()
    {
        return Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
    }

    private void WriteLog(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var logPath = GetLogFilePath();
                var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message) => WriteLog("ERROR", message);
    public void LogError(string message, Exception ex) => WriteLog("ERROR", $"{message}: {ex}");
}
