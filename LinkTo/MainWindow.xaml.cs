using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Helpers;
using LinkTo.Services;
using LinkTo.Views;
using WinRT.Interop;

namespace LinkTo;

/// <summary>
/// Main application window with navigation
/// </summary>
public sealed partial class MainWindow : Window
{
    private string? _initialSourcePath;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set window size and center on screen
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        var windowWidth = 800;
        var windowHeight = 780;
        appWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
        
        // Center window on screen
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var centerX = (workArea.Width - windowWidth) / 2 + workArea.X;
            var centerY = (workArea.Height - windowHeight) / 2 + workArea.Y;
            appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
        }

        // Apply localized strings
        ApplyLocalization();

        // Select first item by default
        NavView.SelectedItem = NavItem_CreateLink;
    }

    /// <summary>
    /// Set initial source path from command line arguments
    /// </summary>
    public void SetInitialSourcePath(string path)
    {
        _initialSourcePath = path;
        
        // If already navigated to CreateLinkPage, update it
        if (ContentFrame.Content is CreateLinkPage createLinkPage)
        {
            createLinkPage.SetSourcePath(path);
        }
    }

    private void ApplyLocalization()
    {
        try
        {
            NavItem_CreateLink.Content = LocalizationHelper.GetString("Tab_CreateLink");
            NavItem_History.Content = LocalizationHelper.GetString("Tab_History");
            NavItem_Settings.Content = LocalizationHelper.GetString("Tab_Settings");
            NavItem_Help.Content = LocalizationHelper.GetString("Tab_Help");
            NavItem_About.Content = LocalizationHelper.GetString("Tab_About");
        }
        catch
        {
            // Ignore errors
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string? tag)
    {
        Type? pageType = tag switch
        {
            "CreateLink" => typeof(CreateLinkPage),
            "History" => typeof(HistoryPage),
            "Settings" => typeof(SettingsPage),
            "Help" => typeof(HelpPage),
            "About" => typeof(AboutPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);

            // Pass initial source path to CreateLinkPage
            if (pageType == typeof(CreateLinkPage) && !string.IsNullOrEmpty(_initialSourcePath))
            {
                if (ContentFrame.Content is CreateLinkPage createLinkPage)
                {
                    createLinkPage.SetSourcePath(_initialSourcePath);
                    _initialSourcePath = null; // Clear after use
                }
            }
        }
    }
}
