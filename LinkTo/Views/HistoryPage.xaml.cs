using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Services;

namespace LinkTo.Views;

/// <summary>
/// Page for viewing and managing link history
/// </summary>
public sealed partial class HistoryPage : Page
{
    private readonly ResourceLoader _resourceLoader;

    public HistoryPage()
    {
        InitializeComponent();
        _resourceLoader = new ResourceLoader();
        ApplyLocalization();
        LoadHistory();
    }

    private void ApplyLocalization()
    {
        try
        {
            EmptyMessage.Text = _resourceLoader.GetString("History_Empty");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void LoadHistory()
    {
        var history = ConfigService.Instance.Config.LinkHistory.ToList();
        HistoryListView.ItemsSource = history;

        EmptyMessage.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryListView.Visibility = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var entry = ConfigService.Instance.Config.LinkHistory.FirstOrDefault(h => h.Id == id);
            if (entry == null) return;

            // Confirm deletion
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Dialog_Confirm"),
                Content = _resourceLoader.GetString("History_DeleteConfirm"),
                PrimaryButtonText = _resourceLoader.GetString("Dialog_Yes"),
                CloseButtonText = _resourceLoader.GetString("Dialog_No"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            // Delete the link file
            var deleteResult = LinkService.Instance.DeleteLink(entry.LinkPath);
            
            // Remove from history regardless of file deletion result
            ConfigService.Instance.RemoveLinkHistory(id);
            LoadHistory();

            if (!deleteResult.Success && deleteResult.Error != null)
            {
                // Show warning but don't block - history entry is already removed
                var warningDialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("Dialog_Warning"),
                    Content = deleteResult.Error,
                    CloseButtonText = _resourceLoader.GetString("Dialog_OK"),
                    XamlRoot = this.XamlRoot
                };
                await warningDialog.ShowAsync();
            }
        }
    }
}
