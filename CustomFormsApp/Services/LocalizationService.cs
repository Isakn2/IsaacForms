// Services/LocalizationService.cs
using Microsoft.JSInterop;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace CustomFormsApp.Services
{
    public class LocalizationService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _currentLanguage = "en-US"; // Default to English
        private bool _isInitialized = false;

        public event Func<Task>? LanguageChanged;

        public LocalizationService(IJSRuntime jsRuntime, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
        {
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentLanguage => _currentLanguage;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // First try to get language from the HTTP context (cookie)
                var currentCulture = CultureInfo.CurrentCulture.Name;
                
                try
                {
                    // Only try JavaScript interop if not prerendering
                    // This prevents the "JavaScript interop calls cannot be issued at this time" error
                    if (OperatingSystem.IsBrowser())
                    {
                        // Then try to get from JavaScript localStorage
                        var jsLanguage = await _jsRuntime.InvokeAsync<string>("appInterop.localization.initialize");
                        
                        // If JS and cookie don't match, prefer the JS one as it's the user's explicit choice
                        if (!string.IsNullOrEmpty(jsLanguage) && jsLanguage != currentCulture)
                        {
                            _currentLanguage = jsLanguage;
                            // Update the culture cookie to match
                            await SetCultureCookieAsync(jsLanguage);
                        }
                        else
                        {
                            _currentLanguage = currentCulture;
                        }
                    }
                    else
                    {
                        _currentLanguage = currentCulture;
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
                {
                    // This is expected during prerendering - just use the server culture
                    _currentLanguage = currentCulture;
                    Console.WriteLine("Skipping JS interop during prerendering");
                }
                catch (Exception ex)
                {
                    // Other JS errors - use server culture
                    Console.WriteLine($"JS error during language init: {ex.Message}");
                    _currentLanguage = currentCulture;
                }
                
                Console.WriteLine($"LocalizationService: Initialized language to '{_currentLanguage}'");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing language: {ex.Message}");
                // Fall back to default language
                _currentLanguage = "en-US";
                _isInitialized = true;
            }
        }

        public async Task<string> GetCurrentLanguageAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            try
            {
                // Get the latest language from JavaScript - this is the source of truth for user preference
                _currentLanguage = await _jsRuntime.InvokeAsync<string>("appInterop.localization.get");
                return _currentLanguage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting language: {ex.Message}");
                return _currentLanguage; // Fall back to cached value
            }
        }

        public async Task SetLanguageAsync(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                throw new ArgumentException("Language code cannot be empty", nameof(languageCode));
            }

            if (languageCode == _currentLanguage && _isInitialized)
            {
                return; // No change needed
            }

            try
            {
                // Set in JavaScript localStorage
                bool success = await _jsRuntime.InvokeAsync<bool>("appInterop.localization.set", languageCode);
                
                if (success)
                {
                    _currentLanguage = languageCode;
                    
                    // Set the culture cookie
                    await SetCultureCookieAsync(languageCode);
                    
                    // Notify listeners
                    await OnLanguageChangedAsync();
                    
                    Console.WriteLine($"LocalizationService: Changed language to '{_currentLanguage}'");
                    
                    // We're removing the forced page refresh here
                    // Let components respond to the language change event instead
                }
                else
                {
                    Console.WriteLine($"Failed to set language to '{languageCode}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting language: {ex.Message}");
                throw;
            }
        }
        
        private async Task SetCultureCookieAsync(string culture)
        {
            try
            {
                // This updates the ASP.NET Core culture cookie
                await _jsRuntime.InvokeVoidAsync(
                    "eval", 
                    $"document.cookie = '{CookieRequestCultureProvider.DefaultCookieName}=c={culture}|uic={culture}; path=/; expires=' + new Date(new Date().setFullYear(new Date().getFullYear() + 1)).toUTCString();"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting culture cookie: {ex.Message}");
            }
        }

        private async Task OnLanguageChangedAsync()
        {
            if (LanguageChanged != null)
            {
                await LanguageChanged.Invoke();
            }
        }
    }
}