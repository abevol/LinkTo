using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Helpers;
using LinkTo.Services;

namespace LinkTo.Views;

/// <summary>
/// Settings page for language and shell integration
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ResourceLoader _resourceLoader;
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        _resourceLoader = new ResourceLoader();
        ApplyLocalization();
        LoadSettings();
        _isInitializing = false;
    }

    private void ApplyLocalization()
    {
        try
        {
            LanguageLabel.Text = _resourceLoader.GetString("Settings_Language");
            ShellMenuLabel.Text = _resourceLoader.GetString("Settings_ShellMenu");
            ShellMenuDescription.Text = _resourceLoader.GetString("Settings_ShellMenuDesc");
            AdminWarningText.Text = _resourceLoader.GetString("Settings_RequiresAdmin");
            EnglishItem.Content = _resourceLoader.GetString("Language_English");
            ChineseItem.Content = _resourceLoader.GetString("Language_Chinese");
        }
        catch
        {
            // Use default English if resource loading fails
        }
    }

    private void LoadSettings()
    {
        // Language
        var currentLanguage = ConfigService.Instance.Language;
        LanguageComboBox.SelectedItem = currentLanguage.StartsWith("zh") ? ChineseItem : EnglishItem;

        // Shell menu
        ShellMenuToggle.IsOn = ShellIntegrationService.Instance.IsRegistered();

        // Show admin warning if not running as admin
        AdminWarningPanel.Visibility = AdminHelper.IsRunningAsAdmin() ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string language)
        {
            if (ConfigService.Instance.Language != language)
            {
                ConfigService.Instance.Language = language;

                // Show restart prompt
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("Dialog_Confirm"),
                    Content = _resourceLoader.GetString("Settings_RestartRequired"),
                    PrimaryButtonText = _resourceLoader.GetString("Dialog_OK"),
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }

    private async void ShellMenuToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var isOn = ShellMenuToggle.IsOn;
        var currentState = ShellIntegrationService.Instance.IsRegistered();

        if (isOn == currentState) return;

        // Check if admin is required
        if (!AdminHelper.IsRunningAsAdmin())
        {
            var confirmDialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Dialog_Warning"),
                Content = _resourceLoader.GetString("Dialog_AdminRequired"),
                PrimaryButtonText = _resourceLoader.GetString("Dialog_Yes"),
                CloseButtonText = _resourceLoader.GetString("Dialog_No"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var confirmResult = await confirmDialog.ShowAsync();
            if (confirmResult == ContentDialogResult.Primary)
            {
                AdminHelper.RestartAsAdmin();
                Application.Current.Exit();
            }
            else
            {
                // Revert toggle
                _isInitializing = true;
                ShellMenuToggle.IsOn = currentState;
                _isInitializing = false;
            }
            return;
        }

        // Perform the operation
        var result = isOn
            ? ShellIntegrationService.Instance.Register()
            : ShellIntegrationService.Instance.Unregister();

        if (!result.Success)
        {
            var errorDialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Dialog_Error"),
                Content = result.Error,
                CloseButtonText = _resourceLoader.GetString("Dialog_OK"),
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();

            // Revert toggle
            _isInitializing = true;
            ShellMenuToggle.IsOn = currentState;
            _isInitializing = false;
        }
    }
}
