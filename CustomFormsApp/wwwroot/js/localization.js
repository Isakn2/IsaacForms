// Localization JavaScript interop functions
window.appInterop = window.appInterop || {};
window.appInterop.localization = {
    // Initialize localization and get current culture
    initialize: function () {
        // Try to get from localStorage first
        const savedLanguage = localStorage.getItem('app_language');
        
        // If saved language exists, use it
        if (savedLanguage) {
            return savedLanguage;
        }
        
        // Otherwise try to get from browser settings
        const browserLanguage = navigator.language;
        // Default to English if not found
        const defaultLanguage = 'en-US';
        
        // Save it for future use
        localStorage.setItem('app_language', defaultLanguage);
        
        return defaultLanguage;
    },
    
    // Get current language
    get: function () {
        return localStorage.getItem('app_language') || 'en-US';
    },
    
    // Set language
    set: function (languageCode) {
        try {
            localStorage.setItem('app_language', languageCode);
            return true;
        } catch (error) {
            console.error('Failed to set language:', error);
            return false;
        }
    }
};