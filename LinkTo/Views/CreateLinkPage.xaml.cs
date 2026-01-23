using System;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
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


    public CreateLinkPage()
    {
        InitializeComponent();

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
        UpdateWorkingDirectoryDefault();
        UpdateHardLinkAvailability();
    }

    private void ApplyLocalization()
    {
        try
        {
            SourceGroupHeader.Text = LocalizationHelper.GetString("Group_Source");
            TargetGroupHeader.Text = LocalizationHelper.GetString("Group_Target");
            LinkNameGroupHeader.Text = LocalizationHelper.GetString("Group_LinkName");
            LinkTypeGroupHeader.Text = LocalizationHelper.GetString("Group_LinkType");
            DragDropHint.Text = LocalizationHelper.GetString("Hint_DragDrop");
            BrowseFileButton.Content = LocalizationHelper.GetString("Button_BrowseFile");
            BrowseFolderButton.Content = LocalizationHelper.GetString("Button_BrowseFolder");
            BrowseTargetButton.Content = LocalizationHelper.GetString("Button_Browse");
            CreateLinkButton.Content = LocalizationHelper.GetString("Button_CreateLink");
            SymbolicLinkRadio.Content = LocalizationHelper.GetString("LinkType_Symbolic");
            HardLinkRadio.Content = LocalizationHelper.GetString("LinkType_Hard");
            BatchLinkRadio.Content = LocalizationHelper.GetString("LinkType_Batch");
            ShortcutLinkRadio.Content = LocalizationHelper.GetString("LinkType_Shortcut");
            WorkingDirHeader.Text = LocalizationHelper.GetString("Group_WorkingDirectory");
            CommonDirsHeader.Text = LocalizationHelper.GetString("CommonDirectories");
            
            // TextBox PlaceholderText
            SourcePathTextBox.PlaceholderText = LocalizationHelper.GetString("Placeholder_SelectSource");
            TargetPathTextBox.PlaceholderText = LocalizationHelper.GetString("Placeholder_SelectTarget");
            WorkingDirTextBox.PlaceholderText = LocalizationHelper.GetString("Placeholder_SelectWorkingDir");
            LinkNameTextBox.PlaceholderText = LocalizationHelper.GetString("Placeholder_LinkName");
            BrowseWorkingDirButton.Content = LocalizationHelper.GetString("Button_Browse");
            
            if (ExtendedOptionsHeader != null)
            {
                ExtendedOptionsHeader.Text = LocalizationHelper.GetString("Group_ExtendedOptions");
            }

            if (MigrateDataCheckBox != null)
            {
                MigrateDataCheckBox.Content = LocalizationHelper.GetString("MigrateDataCheckBox.Content");
            }
            if (DataMigrationDescription != null)
            {
                DataMigrationDescription.Text = LocalizationHelper.GetString("DataMigrationDescription.Text");
            }
            if (ProgressStatusText != null)
            {
                ProgressStatusText.Text = LocalizationHelper.GetString("Status_Processing");
            }
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void SetLoading(bool isLoading)
    {
        ProgressOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        CreateLinkButton.IsEnabled = !isLoading;

        // Try to disable NavigationView during processing to prevent navigation
        if (App.MainWindow?.Content is Grid mainGrid)
        {
            var navView = mainGrid.Children.OfType<NavigationView>().FirstOrDefault();
            if (navView != null) navView.IsEnabled = !isLoading;
        }
    }

    private void LoadCommonDirectories()
    {
        CommonDirsList.Items.Clear();
        var dirs = ConfigService.Instance.Config.CommonDirectories;
        if (dirs != null)
        {
            foreach (var dir in dirs)
            {
                // Create UI elements manually to avoid AOT issues with ItemsSource binding
                var grid = new Grid
                {
                    ColumnSpacing = 8,
                    Padding = new Thickness(4)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var textBlock = new TextBlock
                {
                    Text = dir,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                // Tapped handler removed, moved to ItemClick
                grid.Tag = dir;
                grid.PointerEntered += CommonDir_PointerEntered;
                grid.PointerExited += CommonDir_PointerExited;
                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);

                var deleteButton = new Button
                {
                    Content = "\uE74D",
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    Tag = dir
                };
                deleteButton.Click += (s, e) =>
                {
                    if (s is Button btn && btn.Tag is string path)
                    {
                        ConfigService.Instance.RemoveCommonDirectory(path);
                        LoadCommonDirectories();
                    }
                };
                Grid.SetColumn(deleteButton, 1);
                grid.Children.Add(deleteButton);

                CommonDirsList.Items.Add(grid);
            }
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
                UpdateWorkingDirectoryDefault();
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
            UpdateWorkingDirectoryDefault();
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
            UpdateWorkingDirectoryDefault();
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

    private async void BrowseWorkingDir_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow == null) return;

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            WorkingDirTextBox.Text = folder.Path;
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

    private void CommonDirsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Grid grid && grid.Tag is string path)
        {
            TargetPathTextBox.Text = path;
            UpdateHardLinkAvailability();
        }
    }

    private void CommonDir_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private void CommonDir_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        this.ProtectedCursor = null;
    }

    #endregion

    private void TargetPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateHardLinkAvailability();
    }

    private void LinkType_Checked(object sender, RoutedEventArgs e)
    {
        UpdateWorkingDirectoryVisibility();
        UpdateLinkName();
    }

    private void UpdateWorkingDirectoryVisibility()
    {
        if (WorkingDirectoryBorder == null || BatchLinkRadio == null || ShortcutLinkRadio == null) return; // Not initialized yet

        bool isBatch = BatchLinkRadio.IsChecked == true;
        bool isShortcut = ShortcutLinkRadio.IsChecked == true;
        
        WorkingDirectoryBorder.Visibility = (isBatch || isShortcut) ? Visibility.Visible : Visibility.Collapsed;
        
        if (isBatch)
        {
            UpdateWorkingDirectoryDefault();
        }
        else if (isShortcut && string.IsNullOrEmpty(WorkingDirTextBox.Text))
        {
            UpdateWorkingDirectoryDefault();
        }
    }

    private void UpdateWorkingDirectoryDefault()
    {
        if (BatchLinkRadio != null && BatchLinkRadio.IsChecked == true)
        {
            WorkingDirTextBox.Text = string.Empty;
            return;
        }

        var sourcePath = SourcePathTextBox.Text;
        if (!string.IsNullOrEmpty(sourcePath))
        {
            try 
            {
                if (File.Exists(sourcePath))
                {
                    WorkingDirTextBox.Text = Path.GetDirectoryName(sourcePath);
                }
                else if (Directory.Exists(sourcePath))
                {
                    WorkingDirTextBox.Text = sourcePath;
                }
            }
            catch {}
        }
    }

    private void UpdateLinkName()
    {
        var sourcePath = SourcePathTextBox.Text;
        if (string.IsNullOrEmpty(sourcePath)) return;

        if (BatchLinkRadio?.IsChecked == true)
        {
            LinkNameTextBox.Text = Path.GetFileNameWithoutExtension(sourcePath) + ".bat";
        }
        else if (ShortcutLinkRadio?.IsChecked == true)
        {
            LinkNameTextBox.Text = Path.GetFileNameWithoutExtension(sourcePath) + ".lnk";
        }
        else
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
            HardLinkInfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var (canUse, reason) = LinkService.Instance.CanCreateHardLink(sourcePath, targetDir);
        HardLinkRadio.IsEnabled = canUse;

        if (!canUse && reason != null)
        {
            HardLinkInfoText.Text = reason;
            HardLinkInfoPanel.Visibility = Visibility.Visible;
            
            // If hard link was selected, switch to symbolic
            if (HardLinkRadio.IsChecked == true)
            {
                SymbolicLinkRadio.IsChecked = true;
            }
        }
        else
        {
            HardLinkInfoPanel.Visibility = Visibility.Collapsed;
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
            await ShowErrorDialog(LocalizationHelper.GetString("Error_SourceRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(targetDir))
        {
            await ShowErrorDialog(LocalizationHelper.GetString("Error_TargetRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(linkName))
        {
            await ShowErrorDialog(LocalizationHelper.GetString("Error_LinkNameRequired"));
            return;
        }

        if (!Directory.Exists(targetDir))
        {
            await ShowErrorDialog(LocalizationHelper.GetString("Error_TargetNotExist"));
            return;
        }

        LinkType linkType;
        if (HardLinkRadio.IsChecked == true) linkType = LinkType.Hard;
        else if (BatchLinkRadio.IsChecked == true) linkType = LinkType.Batch;
        else if (ShortcutLinkRadio.IsChecked == true) linkType = LinkType.Shortcut;
        else linkType = LinkType.Symbolic;

        var workingDir = WorkingDirTextBox.Text;
        var linkPath = Path.Combine(targetDir, linkName);
        bool migrateData = MigrateDataCheckBox?.IsChecked == true;

        // Append extension for Batch or Shortcut if not present
        if (linkType == LinkType.Batch && !linkPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
        {
            linkPath += ".bat";
            linkName += ".bat";
        }
        else if (linkType == LinkType.Shortcut && !linkPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            linkPath += ".lnk";
            linkName += ".lnk";
        }

        // Confirm migration (since it moves files)
        if (migrateData)
        {
            var migrationConfirm = await ShowConfirmDialog(
                LocalizationHelper.GetString("Dialog_Warning"),
                LocalizationHelper.GetString("Dialog_MigrationConfirm"));

            if (!migrationConfirm) return;
        }

        // Check if target exists
        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            var confirmResult = await ShowConfirmDialog(
                LocalizationHelper.GetString("Dialog_Warning"),
                LocalizationHelper.GetString("Dialog_OverwriteConfirm"));

            if (!confirmResult)
            {
                return;
            }

            // Delete existing
            try
            {
                if (Directory.Exists(linkPath))
                    Directory.Delete(linkPath, true); // true for recursive delete if it's a dir
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
                LocalizationHelper.GetString("Dialog_Warning"),
                LocalizationHelper.GetString("Dialog_AdminRequired"));

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
        try
        {
            SetLoading(true);

            var result = await System.Threading.Tasks.Task.Run(() => 
                LinkService.Instance.CreateLink(sourcePath, targetDir, linkName, linkType, workingDir, migrateData));

            if (result.Success)
            {
                LoadCommonDirectories(); // Refresh common directories

                await ShowSuccessDialog(LocalizationHelper.GetString("Dialog_LinkCreated"));

                // Clear inputs
                SourcePathTextBox.Text = string.Empty;
                LinkNameTextBox.Text = string.Empty;
                if (MigrateDataCheckBox != null) MigrateDataCheckBox.IsChecked = false;
            }
            else
            {
                await ShowErrorDialog(result.Error ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    #region Dialogs

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationHelper.GetString("Dialog_Error"),
            Content = message,
            CloseButtonText = LocalizationHelper.GetString("Dialog_OK"),
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ShowSuccessDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationHelper.GetString("Dialog_Success"),
            Content = message,
            CloseButtonText = LocalizationHelper.GetString("Dialog_OK"),
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
            PrimaryButtonText = LocalizationHelper.GetString("Dialog_Yes"),
            CloseButtonText = LocalizationHelper.GetString("Dialog_No"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    #endregion
}
