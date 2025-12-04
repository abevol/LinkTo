using System;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;
using LinkTo.Models;
using LinkTo.Services;
using LinkTo.Helpers;

namespace LinkTo.Views;



/// <summary>
/// Page for creating symbolic and hard links
/// </summary>
public sealed partial class CreateLinkPage : Page
{
    private readonly ResourceLoader _resourceLoader;

    public CreateLinkPage()
    {
        InitializeComponent();
        _resourceLoader = new ResourceLoader();
        ApplyLocalization();
        UpdateHardLinkAvailability();
        this.Loaded += CreateLinkPage_Loaded;
    }

    private void CreateLinkPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCommonDirectories();
    }

    /// <summary>
    /// Set source path (called from MainWindow for command-line arguments)
    /// </summary>
    public void SetSourcePath(string path)
    {
        SourcePathTextBox.Text = path;
        UpdateLinkName();
        UpdateHardLinkAvailability();
    }

    private void ApplyLocalization()
    {
        try
        {
            SourceGroupHeader.Text = _resourceLoader.GetString("Group_Source");
            TargetGroupHeader.Text = _resourceLoader.GetString("Group_Target");
            LinkNameGroupHeader.Text = _resourceLoader.GetString("Group_LinkName");
            LinkTypeGroupHeader.Text = _resourceLoader.GetString("Group_LinkType");
            DragDropHint.Text = _resourceLoader.GetString("Hint_DragDrop");
            BrowseFileButton.Content = _resourceLoader.GetString("Button_BrowseFile");
            BrowseFolderButton.Content = _resourceLoader.GetString("Button_BrowseFolder");
            BrowseTargetButton.Content = _resourceLoader.GetString("Button_Browse");
            CreateLinkButton.Content = _resourceLoader.GetString("Button_CreateLink");
            SymbolicLinkRadio.Content = _resourceLoader.GetString("LinkType_Symbolic");
            HardLinkRadio.Content = _resourceLoader.GetString("LinkType_Hard");
            CommonDirsHeader.Text = _resourceLoader.GetString("CommonDirectories");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void LoadCommonDirectories()
    {
        var dirs = ConfigService.Instance.Config.CommonDirectories;
        if (dirs != null)
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<object>(
                dirs.Select(d => new CommonDirItem { Path = d })
            );
            CommonDirsList.ItemsSource = items;
        }
    }

    #region Drag and Drop

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Link;
        }
    }

    private async void Page_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                SourcePathTextBox.Text = items[0].Path;
                UpdateLinkName();
                UpdateHardLinkAvailability();
            }
        }
    }

    #endregion

    #region Browse Buttons

    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow == null) return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            SourcePathTextBox.Text = file.Path;
            UpdateLinkName();
            UpdateHardLinkAvailability();
        }
    }

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow == null) return;

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            SourcePathTextBox.Text = folder.Path;
            UpdateLinkName();
            UpdateHardLinkAvailability();
        }
    }

    private async void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow == null) return;

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            TargetPathTextBox.Text = folder.Path;
            UpdateHardLinkAvailability();
        }
    }

    #endregion

    #region Common Directories

    private void CommonDir_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is CommonDirItem item)
        {
            TargetPathTextBox.Text = item.Path;
            UpdateHardLinkAvailability();
        }
    }

    private void DeleteCommonDir_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CommonDirItem item)
        {
            ConfigService.Instance.RemoveCommonDirectory(item.Path);
            LoadCommonDirectories();
        }
    }

    #endregion

    private void TargetPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateHardLinkAvailability();
    }

    private void UpdateLinkName()
    {
        var sourcePath = SourcePathTextBox.Text;
        if (!string.IsNullOrEmpty(sourcePath))
        {
            LinkNameTextBox.Text = Path.GetFileName(sourcePath);
        }
    }

    private void UpdateHardLinkAvailability()
    {
        var sourcePath = SourcePathTextBox.Text;
        var targetDir = TargetPathTextBox.Text;

        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetDir))
        {
            HardLinkRadio.IsEnabled = true;
            HardLinkInfoBar.IsOpen = false;
            return;
        }

        var (canUse, reason) = LinkService.Instance.CanCreateHardLink(sourcePath, targetDir);
        HardLinkRadio.IsEnabled = canUse;

        if (!canUse && reason != null)
        {
            HardLinkInfoBar.Message = reason;
            HardLinkInfoBar.IsOpen = true;
            
            // If hard link was selected, switch to symbolic
            if (HardLinkRadio.IsChecked == true)
            {
                SymbolicLinkRadio.IsChecked = true;
            }
        }
        else
        {
            HardLinkInfoBar.IsOpen = false;
        }
    }

    private async void CreateLink_Click(object sender, RoutedEventArgs e)
    {
        // Validation
        var sourcePath = SourcePathTextBox.Text;
        var targetDir = TargetPathTextBox.Text;
        var linkName = LinkNameTextBox.Text;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            await ShowErrorDialog(_resourceLoader.GetString("Error_SourceRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(targetDir))
        {
            await ShowErrorDialog(_resourceLoader.GetString("Error_TargetRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(linkName))
        {
            await ShowErrorDialog(_resourceLoader.GetString("Error_LinkNameRequired"));
            return;
        }

        if (!Directory.Exists(targetDir))
        {
            await ShowErrorDialog(_resourceLoader.GetString("Error_TargetNotExist"));
            return;
        }

        var linkType = HardLinkRadio.IsChecked == true ? LinkType.Hard : LinkType.Symbolic;
        var linkPath = Path.Combine(targetDir, linkName);

        // Check if target exists
        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            var confirmResult = await ShowConfirmDialog(
                _resourceLoader.GetString("Dialog_Warning"),
                _resourceLoader.GetString("Dialog_OverwriteConfirm"));

            if (!confirmResult)
            {
                return;
            }

            // Delete existing
            try
            {
                if (Directory.Exists(linkPath))
                    Directory.Delete(linkPath, false);
                else
                    File.Delete(linkPath);
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
                return;
            }
        }

        // Check if elevation is needed for symbolic link
        if (linkType == LinkType.Symbolic && LinkService.Instance.RequiresElevationForSymbolicLink())
        {
            var elevateResult = await ShowConfirmDialog(
                _resourceLoader.GetString("Dialog_Warning"),
                _resourceLoader.GetString("Dialog_AdminRequired"));

            if (elevateResult)
            {
                AdminHelper.RestartAsAdmin($"\"{sourcePath}\"");
                Application.Current.Exit();
                return;
            }
            else
            {
                return;
            }
        }

        // Create the link
        var result = LinkService.Instance.CreateLink(sourcePath, targetDir, linkName, linkType);

        if (result.Success)
        {
            LoadCommonDirectories(); // Refresh common directories

            await ShowSuccessDialog(_resourceLoader.GetString("Dialog_LinkCreated"));

            // Clear inputs
            SourcePathTextBox.Text = string.Empty;
            LinkNameTextBox.Text = string.Empty;
        }
        else
        {
            await ShowErrorDialog(result.Error ?? "Unknown error");
        }
    }

    #region Dialogs

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = _resourceLoader.GetString("Dialog_Error"),
            Content = message,
            CloseButtonText = _resourceLoader.GetString("Dialog_OK"),
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ShowSuccessDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = _resourceLoader.GetString("Dialog_Success"),
            Content = message,
            CloseButtonText = _resourceLoader.GetString("Dialog_OK"),
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = _resourceLoader.GetString("Dialog_Yes"),
            CloseButtonText = _resourceLoader.GetString("Dialog_No"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    #endregion
}
