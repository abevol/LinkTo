using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Helpers;

namespace LinkTo.Views;

/// <summary>
/// About page showing application information
/// </summary>
public sealed partial class AboutPage : Page
{


    public AboutPage()
    {
        InitializeComponent();
        InitializeComponent();
        ApplyLocalization();
        LoadVersion();
    }

    private void ApplyLocalization()
    {
        try
        {
            DescriptionText.Text = LocalizationHelper.GetString("About_Description");
            AuthorLabel.Text = LocalizationHelper.GetString("About_Author");
            HomepageLabel.Text = LocalizationHelper.GetString("About_Homepage");
            VersionLabel.Text = LocalizationHelper.GetString("About_Version");
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
