// wwwroot/js/themeManager.js
export function initializeTheme() {
    try {
        const savedTheme = localStorage.getItem('theme') || 
            (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        
        document.documentElement.setAttribute('data-bs-theme', savedTheme);
        document.documentElement.setAttribute('data-theme', savedTheme);
        
        if (document.body) {
            document.body.classList.remove('dark-theme', 'light-theme');
            document.body.classList.add(`${savedTheme}-theme`);
        }
        
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
        document.documentElement.setAttribute('data-bs-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
        
        if (document.body) {
            document.body.classList.remove('dark-theme', 'light-theme');
            document.body.classList.add(`${theme}-theme`);
        }
        
        console.log('Theme changed:', theme);
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