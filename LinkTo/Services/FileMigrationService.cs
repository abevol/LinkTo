using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LinkTo.Helpers;
using Microsoft.VisualBasic.FileIO;

namespace LinkTo.Services;

/// <summary>
/// Service for handling file and directory migration operations with safety checks and Native Windows UI
/// </summary>
public class FileMigrationService : IMigrationService
{
    private static readonly Lazy<FileMigrationService> _instance = new(() => new FileMigrationService());
    public static FileMigrationService Instance => _instance.Value;

    private FileMigrationService() { }

    #region Win32 Modal Hacks

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    private const uint GW_OWNER = 4;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private const int GWLP_HWNDPARENT = -8;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    #endregion

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> MoveAsync(
        string sourcePath, string destinationPath, bool overwrite = false, IntPtr ownerHwnd = default)
    {
        try
        {
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                return (false, "Source does not exist");
            }

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Note: Microsoft.VisualBasic.FileIO handles most checks and UI dialogs automatically
            // but we still want to maintain the async nature and basic error handling.

            var cts = new CancellationTokenSource();

            if (ownerHwnd != IntPtr.Zero)
            {
                // Disable main window to simulate true modality
                EnableWindow(ownerHwnd, false);

                // Start background thread to forcefully reparent the Windows 10/11 out-of-proc shell dialog
                Task.Run(async () =>
                {
                    uint currentPid = (uint)Environment.ProcessId;

                    while (!cts.IsCancellationRequested)
                    {
                        EnumWindows((hWnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hWnd, out uint windowPid);
                            if (windowPid == currentPid)
                            {
                                var sb = new System.Text.StringBuilder(256);
                                GetClassName(hWnd, sb, sb.Capacity);
                                if (sb.ToString() == "OperationStatusWindow")
                                {
                                    IntPtr currentOwner = GetWindow(hWnd, GW_OWNER);
                                    if (currentOwner == IntPtr.Zero || currentOwner != ownerHwnd)
                                    {
                                        SetWindowLongPtr(hWnd, GWLP_HWNDPARENT, ownerHwnd);
                                    }
                                    return false; // Found and handled, stop enumerating
                                }
                            }
                            return true; // Continue enumerating
                        }, IntPtr.Zero);

                        await Task.Delay(50);
                    }
                });
            }

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(sourcePath))
                    {
                        FileSystem.MoveDirectory(
                            sourcePath, 
                            destinationPath, 
                            UIOption.AllDialogs, 
                            UICancelOption.ThrowException);
                    }
                    else
                    {
                        FileSystem.MoveFile(
                            sourcePath, 
                            destinationPath, 
                            UIOption.AllDialogs, 
                            UICancelOption.ThrowException);
                    }
                });
            }
            finally
            {
                cts.Cancel();
                if (ownerHwnd != IntPtr.Zero)
                {
                    EnableWindow(ownerHwnd, true);
                }
            }

            return (true, null);
        }
        catch (OperationCanceledException)
        {
            LogService.Instance.LogInfo($"Migration cancelled by user: {sourcePath} -> {destinationPath}");
            return (false, "USER_CANCELLED");
        }
        catch (UnauthorizedAccessException ex)
        {
            LogService.Instance.LogError($"Migration failed (Access Denied): {sourcePath} -> {destinationPath}", ex);
            return (false, "ERROR_UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Migration failed: {sourcePath} -> {destinationPath}", ex);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> RollbackAsync(
        string currentPath, string originalPath, IntPtr ownerHwnd = default)
    {
        try
        {
            LogService.Instance.LogInfo($"Rolling back migration: {currentPath} -> {originalPath}");
            // Rollback usually doesn't need UI unless it's a large operation, 
            // but for consistency we use MoveAsync which now has UI.
            return await MoveAsync(currentPath, originalPath, overwrite: false, ownerHwnd: ownerHwnd);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Rollback failed: {currentPath} -> {originalPath}", ex);
            return (false, ex.Message);
        }
    }
}
