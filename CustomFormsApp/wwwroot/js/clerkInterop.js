window.clerkInterop = window.clerkInterop || {};

// Enhanced initialization
window.clerkInterop.initializeClerk = async (publishableKey, dotNetRef) => {
    try {
        // Wait for Clerk to be available
        await window.clerkInterop.waitForClerk();
        
        // Initialize only once
        if (!window.Clerk.__initialized) {
            await window.Clerk.load({ 
                publishableKey: publishableKey 
            });
            window.Clerk.__initialized = true;
        }
        
        // Register event handlers
        return window.clerkInterop.registerClerkEventHandlers(dotNetRef);
    } catch (error) {
        console.error("Clerk initialization failed:", error);
        return false;
    }
};
// Add this to clerkInterop.js
window.clerkInterop.isClerkReady = function() {
    return !!(window.Clerk && window.Clerk.loaded);
};

window.clerkInterop.isSignedIn = function() {
    return window.clerkInterop.isClerkReady() && 
           window.Clerk.user && 
           window.Clerk.session;
};

// Revert to property access for loaded, add small delay
window.clerkInterop.waitForClerk = async (timeout = 20000, interval = 100) => {
    console.log(`[${new Date().toISOString()}] waitForClerk: Starting wait (Timeout: ${timeout}ms)...`);
    const start = Date.now();
    // Wait for the Clerk object itself to exist first
    while (!window.Clerk) {
        if (Date.now() - start > timeout) {
            const errorMsg = "waitForClerk: Timeout waiting for window.Clerk object.";
            console.error(`[${new Date().toISOString()}] ${errorMsg}`);
            throw new Error(errorMsg);
        }
        if ((Date.now() - start) % 5000 < interval) { 
             console.log(`[${new Date().toISOString()}] waitForClerk: Still waiting for window.Clerk object...`);
        }
        await new Promise(resolve => setTimeout(resolve, interval));
    }
    console.log(`[${new Date().toISOString()}] waitForClerk: window.Clerk object FOUND after ${Date.now() - start}ms.`);

    // Add a small delay before checking .loaded
    console.log(`[${new Date().toISOString()}] waitForClerk: Adding small delay (50ms) before checking .loaded property...`);
    await new Promise(resolve => setTimeout(resolve, 50)); 

    // Now wait for Clerk to signal it's loaded via the .loaded PROPERTY
    console.log(`[${new Date().toISOString()}] waitForClerk: Now checking for window.Clerk.loaded property...`);
    // *** Reverted condition back to property access ***
    while (!window.Clerk.loaded) { 
        if (Date.now() - start > timeout) {
            const errorMsg = "waitForClerk: Timeout waiting for window.Clerk.loaded property to be true.";
            console.error(`[${new Date().toISOString()}] ${errorMsg} State:`, window.Clerk);
            throw new Error(errorMsg);
        }
        if ((Date.now() - start) % 5000 < interval) { // Log every ~5 seconds
             console.log(`[${new Date().toISOString()}] waitForClerk: Waiting for Clerk.loaded property... Current state:`, window.Clerk);
        }
        await new Promise(resolve => setTimeout(resolve, interval));
    }
    console.log(`[${new Date().toISOString()}] waitForClerk: Clerk is considered loaded (window.Clerk.loaded is true) after ${Date.now() - start}ms.`);
};

window.clerkInterop.initializeAndLoadClerk = async (publishableKey, dotNetRef, timeout = 10000, interval = 100) => { // Added timeout/interval params
    console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: Starting...`);
    const start = Date.now();

    // --- Wait for window.Clerk object ---
    while (!window.Clerk) {
        if (Date.now() - start > timeout) {
            const errorMsg = "initializeAndLoadClerk: Timeout waiting for window.Clerk object.";
            console.error(`[${new Date().toISOString()}] ${errorMsg}`);
            // No throw here, just return false as per original function design
            return false; 
        }
        if ((Date.now() - start) % 5000 < interval) { 
             console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: Still waiting for window.Clerk object...`);
        }
        await new Promise(resolve => setTimeout(resolve, interval));
    }
    console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: window.Clerk object FOUND after ${Date.now() - start}ms.`);
    // --- End Wait ---

    // Original checks (now that window.Clerk exists)
    if (!publishableKey) {
        console.error("PublishableKey is missing for Clerk initialization.");
        return false;
    }
    if (!dotNetRef) {
        console.error(".NET object reference is missing for Clerk initialization.");
        return false;
    }

    try {
        // Use Clerk.load() which initializes and loads necessary components
        // This should be called only once.
        if (!window.Clerk.__internal_loaded) { // Basic check to prevent multiple loads
             console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: Calling window.Clerk.load()...`);
             await window.Clerk.load({ publishableKey: publishableKey });
             window.Clerk.__internal_loaded = true; // Mark as loaded
             console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: Clerk loaded successfully via Clerk.load() after ${Date.now() - start}ms.`);
        } else {
             console.log(`[${new Date().toISOString()}] initializeAndLoadClerk: Clerk.load() already called, skipping.`);
        }

        // This ensures listeners are attached even if initialization happens multiple times.
        // Consider if registerClerkEventHandlers should also wait for Clerk.loaded
        return window.clerkInterop.registerClerkEventHandlers(dotNetRef);
    } catch (error) {
        console.error(`[${new Date().toISOString()}] initializeAndLoadClerk: Clerk.load() error:`, error);
        return false;
    }
};

window.clerkInterop.registerClerkEventHandlers = (dotNetRef) => {
    if (!window.Clerk || !dotNetRef) {
        console.warn("Clerk or dotNetRef not ready - will retry");
        setTimeout(() => window.clerkInterop.registerClerkEventHandlers(dotNetRef), 500);
        return false;
    }
    try {
        // Remove previous listener if any
        if (window.clerkInterop._unsubscribeClerkEvents) {
            window.clerkInterop._unsubscribeClerkEvents();
        }

        // Store the latest dotNetRef for the handler to use
        window.clerkInterop._lastDotNetRef = dotNetRef;

        // Define the handler function
        const handler = (payload) => {
            const user = payload ? payload.user : null;
            const activeDotNetRef = window.clerkInterop._lastDotNetRef;
            if (!activeDotNetRef) {
                console.error("No active .NET reference found for Clerk event callback.");
                return;
            }
            if (user) {
                const userDto = {
                    id: user.id,
                    firstName: user.firstName,
                    lastName: user.lastName,
                    email: user.emailAddresses?.find(e => e.id === user.primaryEmailAddressId)?.emailAddress || null,
                    imageUrl: user.imageUrl,
                    username: user.username
                };
                activeDotNetRef.invokeMethodAsync('OnUserSignedIn', userDto)
                    .catch(err => console.error("Error invoking OnUserSignedIn:", err));
            } else {
                activeDotNetRef.invokeMethodAsync('OnUserSignedOut')
                    .catch(err => console.error("Error invoking OnUserSignedOut:", err));
            }
        };

        // Add the new listener and store the unsubscribe function
        window.clerkInterop._unsubscribeClerkEvents = window.Clerk.addListener(handler);

        console.log("Clerk event listener registered/updated.");
        return true;
    } catch (error) {
        console.error("Error registering Clerk event handlers:", error);
        return false;
    }
};

// --- Clerk User Data and Actions ---

// Function to get the current user details (called from C# InitializeAsync)
window.clerkInterop.getCurrentUser = () => {
    if (!window.clerkInterop.isClerkReady() || !window.Clerk.user) {
        // console.log("getCurrentUser: Clerk not ready or Clerk.user not available.");
        return null;
    }
    try {
        const clerkUser = window.Clerk.user;
        const userDto = {
            id: clerkUser.id,
            firstName: clerkUser.firstName,
            lastName: clerkUser.lastName,
            email: clerkUser.emailAddresses?.find(e => e.id === clerkUser.primaryEmailAddressId)?.emailAddress || null,
            imageUrl: clerkUser.imageUrl,
            username: clerkUser.username
        };
        // console.log("getCurrentUser returning DTO:", userDto);
        return userDto;
    } catch (error) {
        console.error("Error getting current Clerk user:", error);
        return null;
    }
};

// Wrapper function to open the Sign In modal (called from C# OpenSignIn)
window.clerkInterop.openSignIn = (redirectUrl) => {
    if (window.clerkInterop.isClerkReady() && window.Clerk.openSignIn) {
        console.log(`Opening Sign In, redirect: ${redirectUrl}`);
        try {
            // Use redirectUrl with both parameters to ensure redirection works
            window.Clerk.openSignIn({ 
                redirectUrl: redirectUrl,
                redirectUrlComplete: '/Account/CompleteLogin?ReturnUrl=' + encodeURIComponent(redirectUrl || '/') 
            });
        } catch (error) {
            console.error('Error calling Clerk.openSignIn:', error);
        }
    } else {
        console.error('Clerk.openSignIn not available or Clerk not ready.');
    }
};

// Wrapper function to open the Sign Up modal (called from C# OpenSignUp)
window.clerkInterop.openSignUp = (redirectUrl) => {
    if (window.clerkInterop.isClerkReady() && window.Clerk.openSignUp) {
        console.log(`Opening Sign Up, redirect: ${redirectUrl}`);
        try {
            // Using fallbackRedirectUrl instead of the deprecated redirectUrl
            window.Clerk.openSignUp({ fallbackRedirectUrl: redirectUrl });
        } catch (error) {
            console.error('Error calling Clerk.openSignUp:', error);
        }
    } else {
        console.error('Clerk.openSignUp not available or Clerk not ready.');
    }
};

// Wrapper function to sign the user out (called from C# SignOutAsync)
window.clerkInterop.signOut = async () => {
    // Note: C# should rely on the event listener callback (OnUserSignedOut)
    // for state updates, not the return value of this function.
    if (window.clerkInterop.isClerkReady() && window.Clerk.signOut) {
        console.log("Signing out via Clerk.signOut");
        try {
            await window.Clerk.signOut();
            // Return true indicates the call was attempted.
            return true;
        } catch (error) {
            console.error('Error calling Clerk.signOut:', error);
            return false;
        }
    } else {
        console.error('Clerk.signOut not available or Clerk not ready.');
        return false;
    }
};

// Wrapper function to get the session token (called from C# GetTokenAsync)
window.clerkInterop.getToken = async () => {
    if (window.clerkInterop.isClerkReady() && window.Clerk.session) {
        try {
            // console.log("Attempting to get Clerk token...");
            const token = await window.Clerk.session.getToken();
            // console.log("Token received:", token ? "Yes" : "No");
            return token;
        } catch (error) {
            // Common error: User is signed out, session is null.
            if (error.message && error.message.includes('session is null')) {
                 console.log("Cannot get token: User is signed out (session is null).");
            } else {
                 console.error("Error getting Clerk token:", error);
            }
            return null;
        }
    }
    console.error('Clerk.session not available or Clerk not ready for getToken.');
    return null;
};

console.log("clerkInterop.js loaded and initialized.");