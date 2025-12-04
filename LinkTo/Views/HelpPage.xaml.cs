using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace LinkTo.Views;

/// <summary>
/// Help page with information about symbolic and hard links
/// </summary>
public sealed partial class HelpPage : Page
{
    private readonly ResourceLoader _resourceLoader;

    public HelpPage()
    {
        InitializeComponent();
        _resourceLoader = new ResourceLoader();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        try
        {
            HelpTitle.Text = _resourceLoader.GetString("Help_Title");
            SymbolicLinkTitle.Text = _resourceLoader.GetString("Help_SymbolicLink");
            SymbolicLinkDesc.Text = _resourceLoader.GetString("Help_SymbolicLinkDesc");
            HardLinkTitle.Text = _resourceLoader.GetString("Help_HardLink");
            HardLinkDesc.Text = _resourceLoader.GetString("Help_HardLinkDesc");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }
}
