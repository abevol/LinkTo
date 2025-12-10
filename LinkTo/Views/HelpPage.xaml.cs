using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Helpers;

namespace LinkTo.Views;

/// <summary>
/// Help page with information about symbolic and hard links
/// </summary>
public sealed partial class HelpPage : Page
{


    public HelpPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        try
        {
            HelpTitle.Text = LocalizationHelper.GetString("Help_Title");
            SymbolicLinkTitle.Text = LocalizationHelper.GetString("Help_SymbolicLink");
            SymbolicLinkDesc.Text = LocalizationHelper.GetString("Help_SymbolicLinkDesc");
            HardLinkTitle.Text = LocalizationHelper.GetString("Help_HardLink");
            HardLinkDesc.Text = LocalizationHelper.GetString("Help_HardLinkDesc");
            ExecutableNoteTitle.Text = LocalizationHelper.GetString("Help_ExecutableNoteTitle");
            ExecutableNoteDesc.Text = LocalizationHelper.GetString("Help_ExecutableNote");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }
}
