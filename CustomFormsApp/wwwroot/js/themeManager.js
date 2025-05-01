export function initializeTheme() {
    try {
        const savedTheme = getTheme();
        applyTheme(savedTheme);
        console.log('Theme initialized:', savedTheme);
        return savedTheme;
    } catch (e) {
        console.error("Error initializing theme:", e);
        return 'light';
    }
}

export function setTheme(theme) {
    try {
        localStorage.setItem('theme', theme);
        applyTheme(theme);
        console.log('Theme changed to:', theme);
        return true;
    } catch (e) {
        console.error("Error setting theme:", e);
        return false;
    }
}

export function getTheme() {
    try {
        return localStorage.getItem('theme') || 
            (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    } catch (e) {
        console.error("Error getting theme:", e);
        return 'light';
    }
}

// Private helper function
function applyTheme(theme) {
    // Apply to document
    document.documentElement.setAttribute('data-bs-theme', theme);
    document.documentElement.setAttribute('data-theme', theme);
    
    // Apply to body classes
    if (document.body) {
        document.body.classList.remove('dark-theme', 'light-theme');
        document.body.classList.add(`${theme}-theme`);
    }
}

// Listen for system theme changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    // Only auto-switch if user hasn't explicitly chosen a theme
    if (!localStorage.getItem('theme')) {
        setTheme(e.matches ? 'dark' : 'light');
    }
});