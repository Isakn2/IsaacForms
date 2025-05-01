// Themeservice.cs
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class ThemeService
    {
        private string _currentTheme = "light";

        // Change signature to match C# event pattern
        public event EventHandler<string>? OnThemeChanged;

        public string CurrentTheme => _currentTheme;

        public ThemeService()
        {
            // No JSRuntime dependency needed here anymore
        }

        // This field is only needed if you're using static methods
        private static IServiceProvider? _serviceProvider;
        
        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Remove InitializeThemeAsync method - ThemeInitializer handles this now

        public void SetTheme(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                theme = "light";
                
            if (_currentTheme != theme)
            {
                _currentTheme = theme;
                // Use event pattern with sender and args
                OnThemeChanged?.Invoke(this, _currentTheme);
            }
        }

        // Keep this method for JS interop from the browser
        [JSInvokable("UpdateThemeFromJS")]
        public static Task UpdateThemeFromJS(string theme)
        {
            try 
            {
                if (_serviceProvider != null)
                {
                    var service = _serviceProvider.GetService(typeof(ThemeService)) as ThemeService;
                    service?.SetTheme(theme);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error updating theme from JS: {ex.Message}");
                return Task.FromException(ex);
            }
        }
    }
}