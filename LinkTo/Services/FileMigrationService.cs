using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinkTo.Services;

/// <summary>
/// Service for handling file and directory migration operations with safety checks and Native Windows UI.
/// Uses IFileOperation COM interface via unmanaged function pointers for NativeAOT compatibility.
/// </summary>
public class FileMigrationService : IMigrationService
{
    private static readonly Lazy<FileMigrationService> _instance = new(() => new FileMigrationService());
    public static FileMigrationService Instance => _instance.Value;

    private FileMigrationService() { }

    #region COM Constants and P/Invoke

    private static readonly Guid CLSID_FileOperation = new("3ad05575-8857-4850-9277-11b85bdb8e09");
    private static readonly Guid IID_IFileOperation = new("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8");
    private static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    private const uint FOF_NOCONFIRMMKDIR = 0x0200;
    private const uint CLSCTX_ALL = 23;

    // IUnknown vtable:        0=QueryInterface, 1=AddRef, 2=Release
    // IFileOperation vtable:  3=Advise, 4=Unadvise, 5=SetOperationFlags, 6=SetProgressMessage,
    //   7=SetProgressDialog, 8=SetProperties, 9=SetOwnerWindow,
    //   10=ApplyPropertiesToItem, 11=ApplyPropertiesToItems,
    //   12=RenameItem, 13=RenameItems, 14=MoveItem, 15=MoveItems,
    //   16=CopyItem, 17=CopyItems, 18=DeleteItem, 19=DeleteItems,
    //   20=NewItem, 21=PerformOperations, 22=GetAnyOperationsAborted

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    private const uint COINIT_APARTMENTTHREADED = 0x2;

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext,
        ref Guid riid, out IntPtr ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    /// <summary>
    /// Read a raw function pointer from a COM vtable at the given slot.
    /// </summary>
    private static unsafe void* VtSlot(IntPtr comObj, int slot)
    {
        var vtable = *(IntPtr*)comObj;
        return (void*)*(IntPtr*)(vtable + slot * IntPtr.Size);
    }

    /// <summary>
    /// Release a COM object via IUnknown::Release (vtable slot 2).
    /// </summary>
    private static unsafe void SafeRelease(IntPtr comObj)
    {
        if (comObj != IntPtr.Zero)
        {
            ((delegate* unmanaged[Stdcall]<IntPtr, int>)VtSlot(comObj, 2))(comObj);
        }
    }

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

            // IFileOperation COM requires STA thread for proper progress dialog and window ownership
            return await RunOnStaThread(() => ExecuteMove(sourcePath, destinationPath, ownerHwnd));
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
            return await MoveAsync(currentPath, originalPath, overwrite: false, ownerHwnd: ownerHwnd);
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError($"Rollback failed: {currentPath} -> {originalPath}", ex);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Execute the move via IFileOperation using unmanaged function pointers.
    /// Must run on an STA thread.
    /// </summary>
    private static (bool Success, string? Error) ExecuteMove(
        string sourcePath, string destinationPath, IntPtr ownerHwnd)
    {
        var cts = new CancellationTokenSource();

        try
        {
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

            unsafe
            {
                var clsid = CLSID_FileOperation;
                var iidFileOp = IID_IFileOperation;
                var iidShellItem = IID_IShellItem;

                IntPtr pFileOp = IntPtr.Zero;
                IntPtr pSourceItem = IntPtr.Zero;
                IntPtr pDestFolder = IntPtr.Zero;
                IntPtr pszNewName = IntPtr.Zero;

                try
                {
                    // 1. CoCreateInstance → raw IFileOperation*
                    int hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_ALL, ref iidFileOp, out pFileOp);
                    Marshal.ThrowExceptionForHR(hr);

                    // 2. SetOwnerWindow (slot 9) — makes progress dialog modal to main window
                    if (ownerHwnd != IntPtr.Zero)
                    {
                        hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)
                            VtSlot(pFileOp, 9))(pFileOp, ownerHwnd);
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    // 3. SetOperationFlags (slot 5)
                    hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, int>)
                        VtSlot(pFileOp, 5))(pFileOp, FOF_NOCONFIRMMKDIR);
                    Marshal.ThrowExceptionForHR(hr);

                    // 4. Create shell items
                    hr = SHCreateItemFromParsingName(sourcePath, IntPtr.Zero, ref iidShellItem, out pSourceItem);
                    Marshal.ThrowExceptionForHR(hr);

                    var destFolderPath = Path.GetDirectoryName(destinationPath)!;
                    var destFileName = Path.GetFileName(destinationPath);
                    hr = SHCreateItemFromParsingName(destFolderPath, IntPtr.Zero, ref iidShellItem, out pDestFolder);
                    Marshal.ThrowExceptionForHR(hr);

                    // 5. MoveItem (slot 14) — manually marshal string to LPCWSTR
                    pszNewName = Marshal.StringToCoTaskMemUni(destFileName);
                    hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>)
                        VtSlot(pFileOp, 14))(pFileOp, pSourceItem, pDestFolder, pszNewName, IntPtr.Zero);
                    Marshal.ThrowExceptionForHR(hr);

                    // 6. PerformOperations (slot 21)
                    hr = ((delegate* unmanaged[Stdcall]<IntPtr, int>)
                        VtSlot(pFileOp, 21))(pFileOp);
                    Marshal.ThrowExceptionForHR(hr);

                    // 7. GetAnyOperationsAborted (slot 22) — BOOL is 4-byte int
                    int aborted;
                    hr = ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)
                        VtSlot(pFileOp, 22))(pFileOp, &aborted);
                    Marshal.ThrowExceptionForHR(hr);

                    if (aborted != 0)
                    {
                        LogService.Instance.LogInfo($"Migration cancelled by user: {sourcePath} -> {destinationPath}");
                        return (false, "USER_CANCELLED");
                    }

                    return (true, null);
                }
                finally
                {
                    if (pszNewName != IntPtr.Zero) Marshal.FreeCoTaskMem(pszNewName);
                    SafeRelease(pDestFolder);
                    SafeRelease(pSourceItem);
                    SafeRelease(pFileOp);
                }
            }
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80270000) ||
                                    ex.HResult == unchecked((int)0x800704C7))
        {
            LogService.Instance.LogInfo($"Migration cancelled by user: {sourcePath} -> {destinationPath}");
            return (false, "USER_CANCELLED");
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80070005))
        {
            LogService.Instance.LogError($"Migration failed (Access Denied): {sourcePath} -> {destinationPath}");
            return (false, "ERROR_UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError(
                $"Migration failed with HRESULT 0x{ex.HResult:X}: {sourcePath} -> {destinationPath}", ex);
            return (false, $"Shell file operation failed (0x{ex.HResult:X})");
        }
        finally
        {
            cts.Cancel();
            if (ownerHwnd != IntPtr.Zero)
            {
                EnableWindow(ownerHwnd, true);
            }
        }
    }

    /// <summary>
    /// Run a function on a dedicated STA thread.
    /// IFileOperation COM requires STA for proper progress dialog and window ownership.
    /// </summary>
    private static Task<T> RunOnStaThread<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        var thread = new Thread(() =>
        {
            int hrInit = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                if (hrInit >= 0) // S_OK or S_FALSE
                {
                    CoUninitialize();
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
}
