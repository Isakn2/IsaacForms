(function () {
    // Track if Blazor has initialized
    let blazorInitialized = false;
    
    // Theme management
    function getTheme() {
        try {
            const saved = localStorage.getItem('theme');
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            return saved || (prefersDark ? 'dark' : 'light');
        } catch (e) {
            console.error("Error getting theme:", e);
            return 'light';
        }
    }

    function setTheme(theme) {
        try {
            localStorage.setItem('theme', theme);
            document.documentElement.setAttribute('data-bs-theme', theme);
            document.documentElement.setAttribute('data-theme', theme);
            
            if (document.body) {
                document.body.classList.remove('dark-theme', 'light-theme');
                document.body.classList.add(`${theme}-theme`);
            }
            
            // Only try to call Blazor if it's ready
            if (blazorInitialized && window.DotNet) {
                DotNet.invokeMethodAsync('CustomFormsApp', 'UpdateThemeFromJS', theme)
                    .catch(err => console.error("Error updating Blazor theme:", err));
            }
        } catch (e) {
            console.error("Error setting theme:", e);
        }
    }

    // Simplified language functions
    function getLanguage() {
        return localStorage.getItem('language') || 'en-US';
    }

    function setLanguage(lang) {
        localStorage.setItem('language', lang);
        document.documentElement.setAttribute('lang', lang);
    }

    // Utility functions
    function scrollToBottom() {
        window.scrollTo({
            top: document.body.scrollHeight,
            behavior: 'smooth'
        });
    }

    // Expose functions for Blazor interop
    window.getTheme = getTheme;
    window.setTheme = setTheme;
    window.getLanguage = getLanguage;
    window.setLanguage = setLanguage;
    window.scrollToBottom = scrollToBottom;

    // Single initialization function
    function initialize() {
        if (document.readyState !== 'loading') {
            initApp();
        } else {
            document.addEventListener('DOMContentLoaded', initApp);
        }
    }
    
    function initApp() {
        try {
            // Set theme and language
            const theme = getTheme();
            setTheme(theme);
            setLanguage(getLanguage());
            
            // Listen for theme toggle events
            document.addEventListener('click', (e) => {
                if (e.target.closest('.theme-toggle')) {
                    const newTheme = getTheme() === 'dark' ? 'light' : 'dark';
                    setTheme(newTheme);
                }
            });
            
            // Monitor for dark mode changes
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
                if (!localStorage.getItem('theme')) {
                    setTheme(e.matches ? 'dark' : 'light');
                }
            });
            
            // Check if Blazor is available
            if (window.Blazor) {
                blazorInitialized = true;
                console.log("Blazor initialized with theme:", theme);
            } else {
                // Wait for Blazor
                window.addEventListener('blazor-initialized', () => {
                    blazorInitialized = true;
                    console.log("Blazor initialized with theme:", theme);
                });
            }
        } catch (e) {
            console.error("Initialization error:", e);
        }
    }

    // Start initialization
    initialize();
    
    // Monitor when Blazor is ready (belt and suspenders approach)
    window.addEventListener('load', () => {
        if (window.Blazor && !blazorInitialized) {
            blazorInitialized = true;
            console.log("Blazor detected on window load");
        }
    });
})();