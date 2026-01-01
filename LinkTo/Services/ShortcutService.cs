using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace LinkTo.Services;

/// <summary>
/// Service for creating Windows Shortcuts (.lnk)
/// </summary>
public static class ShortcutService
{
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file
    /// </summary>
    public static (bool Success, string? Error) CreateShortcut(string sourcePath, string targetPath, string workingDir)
    {
        try
        {
            LogService.Instance.LogInfo($"Creating shortcut link: {targetPath} -> {sourcePath} (WorkDir: {workingDir})");

            var link = (IShellLinkW)new ShellLink();
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
        catch (Exception ex)
        {
            LogService.Instance.LogError("Shortcut creation failed", ex);
            return (false, ex.Message);
        }
    }
}
