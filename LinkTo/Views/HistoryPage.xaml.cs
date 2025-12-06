using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Models;
using LinkTo.Services;
using LinkTo.Helpers;

namespace LinkTo.Views;

/// <summary>
/// Page for viewing and managing link history
/// </summary>
public sealed partial class HistoryPage : Page
{
    private List<LinkHistoryEntry> _historyData = new();

    public HistoryPage()
    {
        InitializeComponent();
        InitializeComponent();
        ApplyLocalization();
        LoadHistory();
    }

    private void ApplyLocalization()
    {
        try
        {
            EmptyMessage.Text = LocalizationHelper.GetString("History_Empty");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void LoadHistory()
    {
        HistoryListView.Items.Clear();
        _historyData.Clear();
        
        var history = ConfigService.Instance.Config.LinkHistory;
        if (history != null)
        {
            _historyData = history.ToList();
            
            foreach (var entry in _historyData)
            {
                // Create UI elements manually to avoid AOT issues with ItemsSource binding
                var border = new Border
                {
                    Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 4, 0, 4)
                };

                var grid = new Grid { ColumnSpacing = 16 };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var contentStack = new StackPanel { Spacing = 4 };
                Grid.SetColumn(contentStack, 0);

                // Link path row
                var linkPathStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                linkPathStack.Children.Add(new FontIcon { Glyph = "\uE71B", FontSize = 16 });
                linkPathStack.Children.Add(new TextBlock 
                { 
                    Text = entry.LinkPath, 
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontWeight = FontWeights.SemiBold
                });
                contentStack.Children.Add(linkPathStack);

                // Source path row
                var sourceStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                sourceStack.Children.Add(new TextBlock 
                { 
                    Text = LocalizationHelper.GetString("History_Source"), 
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });
                sourceStack.Children.Add(new TextBlock 
                { 
                    Text = entry.SourcePath, 
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });
                contentStack.Children.Add(sourceStack);

                // Type and date row
                var metaStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
                
                var typeStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                typeStack.Children.Add(new TextBlock 
                { 
                    Text = LocalizationHelper.GetString("History_Type"), 
                    Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
                var linkTypeText = entry.LinkType == Models.LinkType.Symbolic 
                    ? LocalizationHelper.GetString("History_LinkType_Symbolic") 
                    : LocalizationHelper.GetString("History_LinkType_Hard");
                typeStack.Children.Add(new TextBlock 
                { 
                    Text = linkTypeText, 
                    Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
                metaStack.Children.Add(typeStack);

                var dateStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                dateStack.Children.Add(new TextBlock 
                { 
                    Text = LocalizationHelper.GetString("History_Created"), 
                    Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
                dateStack.Children.Add(new TextBlock 
                { 
                    Text = entry.CreatedAt.ToString("g"), 
                    Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]
                });
                metaStack.Children.Add(dateStack);
                
                contentStack.Children.Add(metaStack);
                grid.Children.Add(contentStack);

                // Delete button
                var deleteButton = new Button
                {
                    Content = "\uE74D",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Background = new SolidColorBrush(Colors.Transparent),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = entry.Id
                };
                deleteButton.Click += DeleteHistory_Click;
                Grid.SetColumn(deleteButton, 1);
                grid.Children.Add(deleteButton);

                border.Child = grid;
                HistoryListView.Items.Add(border);
            }
        }
        
        EmptyMessage.Visibility = _historyData.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryListView.Visibility = _historyData.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var entry = _historyData.FirstOrDefault(h => h.Id == id);
            if (entry == null) return;

            // Confirm deletion
            var dialog = new ContentDialog
            {
                Title = LocalizationHelper.GetString("Dialog_Confirm"),
                Content = LocalizationHelper.GetString("History_DeleteConfirm"),
                PrimaryButtonText = LocalizationHelper.GetString("Dialog_Yes"),
                CloseButtonText = LocalizationHelper.GetString("Dialog_No"),
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
                    Title = LocalizationHelper.GetString("Dialog_Warning"),
                    Content = deleteResult.Error,
                    CloseButtonText = LocalizationHelper.GetString("Dialog_OK"),
                    XamlRoot = this.XamlRoot
                };
                await warningDialog.ShowAsync();
            }
        }
    }
}
