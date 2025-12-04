using System;
using Microsoft.UI.Xaml;
using LinkTo.Services;

namespace LinkTo;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static MainWindow? _mainWindow;

    public static MainWindow? MainWindow => _mainWindow;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        
        // Initialize logging
        LogService.Instance.LogInfo("Application starting...");
        
        // Handle unhandled exceptions
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogService.Instance.LogError("Unhandled exception", e.Exception);
        e.Handled = true;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();

        // Handle command-line arguments
        var commandLineArgs = Environment.GetCommandLineArgs();
        if (commandLineArgs.Length > 1)
        {
            var sourcePath = commandLineArgs[1];
            if (!string.IsNullOrEmpty(sourcePath))
            {
                LogService.Instance.LogInfo($"Launched with source path: {sourcePath}");
                _mainWindow.SetInitialSourcePath(sourcePath);
            }
        }

        _mainWindow.Activate();
        LogService.Instance.LogInfo("Application launched successfully");
    }
}
