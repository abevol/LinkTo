using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace LinkTo.Services;

/// <summary>
/// Service for creating Windows Shortcuts (.lnk)
/// </summary>
public static partial class ShortcutService
{
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private const uint CLSCTX_INPROC_SERVER = 1;
    private static readonly StrategyBasedComWrappers ComWrappersInstance = new();

    [GeneratedComInterface]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal partial interface IShellLinkW
    {
        void GetPath(IntPtr pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription(IntPtr pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory(IntPtr pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments(IntPtr pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation(IntPtr pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    // Flat declaration of the shell's IPersistFile (IID 0000010b) so the vtable
    // layout stays complete without inheriting from the [ComImport] BCL type.
    [GeneratedComInterface]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    internal partial interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, in Guid riid, out IntPtr ppv);

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file
    /// </summary>
    public static (bool Success, string? Error) CreateShortcut(string sourcePath, string targetPath, string workingDir)
    {
        try
        {
            LogService.Instance.LogInfo($"Creating shortcut link: {targetPath} -> {sourcePath} (WorkDir: {workingDir})");

            int hr = CoCreateInstance(CLSID_ShellLink, IntPtr.Zero, CLSCTX_INPROC_SERVER, IID_IUnknown, out IntPtr pShellLink);
            Marshal.ThrowExceptionForHR(hr);
            try
            {
                var link = (IShellLinkW)ComWrappersInstance.GetOrCreateObjectForComInstance(pShellLink, CreateObjectFlags.UniqueInstance);
                link.SetPath(sourcePath);

                if (!string.IsNullOrWhiteSpace(workingDir))
                {
                    link.SetWorkingDirectory(workingDir);
                }

                var file = (IPersistFile)link;
                file.Save(targetPath, false);

                LogService.Instance.LogInfo("Shortcut created successfully");
                return (true, null);
            }
            finally
            {
                Marshal.Release(pShellLink);
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Shortcut creation failed", ex);
            return (false, ex.Message);
        }
    }
}
