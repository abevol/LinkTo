using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace LinkTo.Views;

/// <summary>
/// About page showing application information
/// </summary>
public sealed partial class AboutPage : Page
{
    private readonly ResourceLoader _resourceLoader;

    public AboutPage()
    {
        InitializeComponent();
        _resourceLoader = new ResourceLoader();
        ApplyLocalization();
        LoadVersion();
    }

    private void ApplyLocalization()
    {
        try
        {
            DescriptionText.Text = _resourceLoader.GetString("About_Description");
            AuthorLabel.Text = _resourceLoader.GetString("About_Author");
            HomepageLabel.Text = _resourceLoader.GetString("About_Homepage");
            VersionLabel.Text = _resourceLoader.GetString("About_Version");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void LoadVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            VersionText.Text = $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
