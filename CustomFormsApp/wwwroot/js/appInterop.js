async function loadThemeModule() {
    try {
        return await import('./themeManager.js');
    } catch (e) {
        console.error('Failed to load theme module:', e);
        return null;
    }
}

// --- Safe Invocation Utility ---
async function safeInvoke(dotNetHelper, methodName, ...args) {
    if (typeof window.isSignalRConnected === 'function' && !window.isSignalRConnected()) {
        return false;
    }

    if (!dotNetHelper) {
        return false;
    }

    try {
        await dotNetHelper.invokeMethodAsync(methodName, ...args);
        return true;
    } catch (error) {
        if (error.message && error.message.includes("JavaScript interop calls cannot be issued")) {
             // .NET object reference likely disposed
        }
        return false;
    }
}

// --- Click Outside Handling ---
const clickOutsideHandlers = new Map();

function handleClickOutside(event) {
    clickOutsideHandlers.forEach((handler, id) => {
        if (handler.element && !handler.element.contains(event.target)) {
            safeInvoke(handler.dotNetHelper, 'InvokeClickOutside');
        }
    });
}

// --- Main Interop Object ---
window.appInterop = {
    // Add a debounce utility to prevent rapid successive calls
    _debounce: (func, wait = 300) => {
        let timeout;
        return function(...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    },

    theme: {
        initialize: () => {
            const savedTheme = localStorage.getItem('theme') ||
                (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
            document.documentElement.setAttribute('data-bs-theme', savedTheme);
            return savedTheme;
        },
        get: () => localStorage.getItem('theme') || 'light',
        set: (theme) => {
            localStorage.setItem('theme', theme);
            document.documentElement.setAttribute('data-bs-theme', theme);
            return true;
        },
        toggle: () => {
            const current = localStorage.getItem('theme') ||
                (document.documentElement.getAttribute('data-bs-theme') || 'light');
            const newTheme = current === 'light' ? 'dark' : 'light';
            localStorage.setItem('theme', newTheme);
            document.documentElement.setAttribute('data-bs-theme', newTheme);
            return newTheme;
        }
    },

    formUtils: {
        saveDraft: (formId, formData) => {
            try {
                localStorage.setItem(formId, formData);
                return true;
            } catch (e) {
                return false;
            }
        },
        getDraft: (formId) => {
             try {
                return localStorage.getItem(formId);
            } catch (e) {
                return null;
            }
        },
        clearDraft: (formId) => {
             try {
                localStorage.removeItem(formId);
                return true;
            } catch (e) {
                return false;
            }
        },
        setupBeforeUnloadHandler: () => {
            window.formBeforeUnloadHandler = function(e) {
                e.preventDefault();
                e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
                return 'You have unsaved changes. Are you sure you want to leave?';
            };
            
            window.removeEventListener('beforeunload', window.formBeforeUnloadHandler);
            window.addEventListener('beforeunload', window.formBeforeUnloadHandler);
            return true;
        },
        
        removeBeforeUnloadHandler: () => {
            window.removeEventListener('beforeunload', window.formBeforeUnloadHandler);
            return true;
        },
        findAnyTemplateDraft: (userId) => {
            try {
                const allKeys = Object.keys(localStorage);
                const templateKeyPattern = `form_builder_draft_template_`;
                const userTemplateKeys = allKeys.filter(k => 
                    k.includes(templateKeyPattern) && k.includes(userId));
                
                if (userTemplateKeys.length > 0) {
                    const mostRecentKey = userTemplateKeys[0];
                    return localStorage.getItem(mostRecentKey);
                }
                
                return null;
            } catch (e) {
                return null;
            }
        },
    }, 

    // Form controls section
    formControls: {
        initCheckboxes: () => {
            try {
                const checkboxes = document.querySelectorAll('input[type="checkbox"]');
                checkboxes.forEach(checkbox => {
                    // Make sure the checked attribute reflects the current state
                    if (checkbox.checked) {
                        checkbox.setAttribute('checked', 'checked');
                    } else {
                        checkbox.removeAttribute('checked');
                    }
                    
                    // Add event listener to update the checked attribute when state changes
                    checkbox.addEventListener('change', function() {
                        if (this.checked) {
                            this.setAttribute('checked', 'checked');
                        } else {
                            this.removeAttribute('checked');
                        }
                    });
                });
                return true;
            } catch (e) {
                console.error('Error initializing checkboxes:', e);
                return false;
            }
        },
        
        updateCheckboxState: (id, isChecked) => {
            const checkbox = document.getElementById(id);
            if (checkbox) {
                checkbox.checked = isChecked;
                if (isChecked) {
                    checkbox.setAttribute('checked', 'checked');
                } else {
                    checkbox.removeAttribute('checked');
                }
                return true;
            }
            return false;
        }
    },

    // Auth section
    auth: {
        isAuthenticated: async () => window.clerkInterop?.isSignedIn ? await window.clerkInterop.isSignedIn() : false,
        getCurrentUser: async () => window.clerkInterop?.getCurrentUser ? await window.clerkInterop.getCurrentUser() : null,
        signIn: async (redirectUrl) => window.clerkInterop?.openSignIn ? await window.clerkInterop.openSignIn(redirectUrl) : Promise.resolve(),
        signUp: async (redirectUrl) => window.clerkInterop?.openSignUp ? await window.clerkInterop.openSignUp(redirectUrl) : Promise.resolve(),
        signOut: async () => window.clerkInterop?.signOut ? await window.clerkInterop.signOut() : Promise.resolve(false),
    }, 

    utils: {
        scrollToTop: () => {
            try {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            } catch(e) { }
        },
        scrollToBottom: () => {
             try {
                window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
            } catch(e) { }
        },
        copyToClipboard: async (text) => {
            if (!navigator.clipboard) {
                return false;
            }
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch (err) {
                return false;
            }
        },
        getBaseUrl: () => {
            const base = document.baseURI.endsWith('/') ? document.baseURI : document.baseURI + '/';
            return base;
        },
        safeRedirect: (path) => {
            const relativePath = path.startsWith('/') ? path.substring(1) : path;
            const baseUrl = window.appInterop.utils.getBaseUrl();
            try {
                const fullUrl = new URL(relativePath, baseUrl).href;
                window.location.href = fullUrl;
            } catch (e) {
                // Fallback
            }
        }
    },

    layout: {
        isSmallScreen: () => window.innerWidth < 992,
        
        setupResizeHandler: (dotNetRef) => {
            if (!dotNetRef) {
                return false;
            }
            
            window._layoutDotNetRef = dotNetRef;
            
            const handleResize = window.appInterop._debounce(() => {
                const isSmall = window.appInterop.layout.isSmallScreen();
                window._layoutDotNetRef.invokeMethodAsync('OnScreenSizeChanged', isSmall);
            }, 250);
            
            window._resizeHandler = handleResize;
            
            window.addEventListener('resize', handleResize);
            
            handleResize();
            
            return true;
        },
        
        removeResizeHandler: () => {
            if (window._resizeHandler) {
                window.removeEventListener('resize', window._resizeHandler);
                window._resizeHandler = null;
            }
            
            if (window._layoutDotNetRef) {
                window._layoutDotNetRef = null;
            }
            
            return true;
        },
        
        closeSidebarOnClickOutside: (sidebarSelector, dotNetRef) => {
            const sidebar = document.querySelector(sidebarSelector);
            if (!sidebar || !dotNetRef) return false;
            
            const handleDocumentClick = (e) => {
                if (window.appInterop.layout.isSmallScreen() && 
                    !sidebar.contains(e.target) && 
                    !e.target.classList.contains('sidebar-toggle')) {
                    dotNetRef.invokeMethodAsync('CloseSidebar');
                }
            };
            
            window._sidebarClickHandler = handleDocumentClick;
            
            document.addEventListener('click', handleDocumentClick);
            
            return true;
        },
        
        removeSidebarClickHandler: () => {
            if (window._sidebarClickHandler) {
                document.removeEventListener('click', window._sidebarClickHandler);
                window._sidebarClickHandler = null;
            }
            
            return true;
        }
    },

    // Sidebar functionality
    sidebar: {
        getElement: () => {
            return document.querySelector('.sidebar');
        },
        
        init: (sidebarElement) => {
            return true;
        }
    },

    // Click Outside Handler Management
    initClickOutsideHandlers: (elementId, dotNetHelper) => {
        if (!dotNetHelper) {
            return null;
        }
        const element = document.getElementById(elementId);
        if (!element) {
            return null;
        }

        const handlerId = elementId || `handler_${Date.now()}`;
        clickOutsideHandlers.set(handlerId, { element, dotNetHelper });

        if (clickOutsideHandlers.size === 1) {
            document.addEventListener('click', handleClickOutside, true);
        }
        return handlerId;
    },

    removeClickOutsideHandlers: (handlerId) => {
        if (clickOutsideHandlers.has(handlerId)) {
            clickOutsideHandlers.delete(handlerId);

            if (clickOutsideHandlers.size === 0) {
                document.removeEventListener('click', handleClickOutside, true);
            }
        }
    }, 

    // --- Localization ---
    localization: {
        initialize: () => {
            let savedLang = localStorage.getItem('language');
            if (!savedLang) {
                savedLang = navigator.language ? navigator.language.split('-')[0] : 'en';
            }
            document.documentElement.setAttribute('lang', savedLang);
            localStorage.setItem('language', savedLang);
            return savedLang;
        },
        get: () => {
            return localStorage.getItem('language') || 'en';
        },
        set: (lang) => {
            if (!lang || typeof lang !== 'string') {
                return false;
            }
            const validLang = lang.split('-')[0];
            localStorage.setItem('language', validLang);
            document.documentElement.setAttribute('lang', validLang);
            return true;
        }
    }
}; 

window.updateDragState = (selector, action, className) => {
    try {
        const element = document.querySelector(selector);
        if (element) {
            if (action === 'add') {
                element.classList.add(className);
            } else {
                element.classList.remove(className);
            }
        }
    } catch (e) {
        // Ignore errors
    }
    return true;
};

window.resetDragState = (selector, className) => {
    try {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => el.classList.remove(className));
    } catch (e) {
        // Ignore errors
    }
    return true;
};

// --- Initialization ---
document.addEventListener('DOMContentLoaded', () => {
    window.appInterop.theme.initialize();
});