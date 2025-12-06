using System;
using Microsoft.Windows.ApplicationModel.Resources;
using LinkTo.Services;

namespace LinkTo.Helpers;

/// <summary>
/// Helper class for retrieving localized strings using a specific language context
/// </summary>
public static class LocalizationHelper
{
    private static ResourceManager? _resourceManager;
    private static ResourceContext? _resourceContext;
    private static ResourceMap? _resourceMap;

    private static void EnsureInitialized()
    {
        if (_resourceManager == null)
        {
            _resourceManager = new ResourceManager();
            _resourceContext = _resourceManager.CreateResourceContext();
            
            // Apply language from config
            var language = ConfigService.Instance.Language;
            if (!string.IsNullOrEmpty(language))
            {
                _resourceContext.QualifierValues["Language"] = language;
            }

            // Get the Resources subtree
            // For WinUI apps, resources are typically under "Resources"
            _resourceMap = _resourceManager.MainResourceMap.TryGetSubtree("Resources") 
                ?? _resourceManager.MainResourceMap;
        }
    }

    public static string GetString(string key)
    {
        try
        {
            EnsureInitialized();
            
            if (_resourceMap != null && _resourceContext != null)
            {
                var candidate = _resourceMap.GetValue(key, _resourceContext);
                if (candidate != null)
                {
                    return candidate.ValueAsString;
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback
            System.Diagnostics.Debug.WriteLine($"Localization failed for key '{key}': {ex.Message}");
        }

        return key;
    }
}
