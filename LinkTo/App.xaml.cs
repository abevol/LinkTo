using System;
using System.Linq;
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
    /// True when the app was started with a command-line argument (e.g. shell context menu)
    /// </summary>
    public static bool LaunchedFromCommandLine { get; private set; }

    /// <summary>
    /// Loading-state preview duration in seconds when started with --test-loading[=seconds]
    /// </summary>
    public static int? TestLoadingSeconds { get; private set; }

    /// <summary>
    /// Theme override for this run when started with --theme=dark|light; null follows the system
    /// </summary>
    public static ElementTheme? TestTheme { get; private set; }

    /// <summary>
    /// Frost scrim tint strength (0..1) when started with --test-tint=&lt;value&gt;
    /// </summary>
    public static double? TestTintOpacity { get; private set; }

    static App()
    {
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
    }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        // Apply language setting
        try 
        {
            var language = ConfigService.Instance.Language;
            if (!string.IsNullOrEmpty(language))
            {
                // Set MRT Core language
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;
                
                // Set .NET culture (important for unpackaged apps and formatting)
                var culture = new System.Globalization.CultureInfo(language);
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            }
        }
        catch (Exception ex)
        {
            // LogService might not be initialized yet, but we can try
            // or just ignore as this is very early
            System.Diagnostics.Debug.WriteLine($"Failed to set application language: {ex.Message}");
        }

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
        // Parse switches before creating the window: MainWindow reads them in its constructor
        var commandLineArgs = Environment.GetCommandLineArgs();
        LaunchedFromCommandLine = commandLineArgs.Length > 1;
        TestLoadingSeconds = ParseTestLoadingSeconds(commandLineArgs);
        TestTheme = ParseTestTheme(commandLineArgs);
        TestTintOpacity = ParseTestTintOpacity(commandLineArgs);

        _mainWindow = new MainWindow();

        var sourcePath = commandLineArgs.Skip(1).FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (!string.IsNullOrEmpty(sourcePath))
        {
            LogService.Instance.LogInfo($"Launched with source path: {sourcePath}");
            _mainWindow.SetInitialSourcePath(sourcePath);
        }

        _mainWindow.Activate();
        LogService.Instance.LogInfo("Application launched successfully");
    }

    private static int? ParseTestLoadingSeconds(string[] args)
    {
        var arg = args.FirstOrDefault(a => a.StartsWith("--test-loading", StringComparison.Ordinal));
        if (arg == null) return null;

        var separator = arg.IndexOf('=');
        if (separator > 0 && int.TryParse(arg[(separator + 1)..], out var seconds) && seconds > 0)
        {
            return seconds;
        }
        return 10;
    }

    private static ElementTheme? ParseTestTheme(string[] args)
    {
        var arg = args.FirstOrDefault(a => a.StartsWith("--theme", StringComparison.Ordinal));
        var value = arg?[(arg.IndexOf('=') + 1)..];
        return value?.ToLowerInvariant() switch
        {
            "dark" => ElementTheme.Dark,
            "light" => ElementTheme.Light,
            _ => null
        };
    }

    private static double? ParseTestTintOpacity(string[] args)
    {
        var arg = args.FirstOrDefault(a => a.StartsWith("--test-tint", StringComparison.Ordinal));
        var value = arg?[(arg.IndexOf('=') + 1)..];
        if (double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var opacity)
            && opacity is >= 0 and <= 1)
        {
            return opacity;
        }
        return null;
    }
}
