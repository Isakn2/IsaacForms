// Monitors and exports SignalR connection state

(function() {
    let connectionState = "Unknown";
    let monitorInterval = null;
    const dotNetRefs = new Set(); // Keep track of .NET objects wanting updates

    function getSignalRConnection() {
        try {
            // Access the internal Blazor connection object
            if (window.Blazor && window.Blazor._internal && window.Blazor._internal.navigationManager && window.Blazor._internal.navigationManager.circuit) {
                return window.Blazor._internal.navigationManager.circuit.connection;
            }
            // Fallback for older/different Blazor versions might be needed
        } catch (e) {
            // console.warn("Error accessing SignalR connection:", e); // Optional: reduce noise
        }
        return null;
    }

    function notifyDotNetRefs(state) {
        if (connectionState === state) return; // No change

        connectionState = state;
        console.log("Connection state changed to:", state);

        // Notify all registered components
        dotNetRefs.forEach(ref => {
            try {
                ref.invokeMethodAsync('UpdateConnectionState', state);
            } catch (error) {
                console.error("Error invoking .NET callback:", error);
                // Consider removing the reference if it's invalid
                // dotNetRefs.delete(ref);
            }
        });

        // Dispatch global events if needed by other JS modules
        window.dispatchEvent(new CustomEvent(`signalr-${state.toLowerCase()}`));
    }

    function checkConnection() {
        const connection = getSignalRConnection();
        let newState = "Unknown";

        if (!connection) {
            newState = "Disconnected"; // Assume disconnected if connection object isn't found
        } else {
            // Map SignalR states to simpler terms
            switch (connection.connectionState) {
                case 'Connected':
                    newState = 'Connected';
                    break;
                case 'Connecting':
                case 'Reconnecting':
                    newState = 'Connecting';
                    break;
                case 'Disconnected':
                default:
                    newState = 'Disconnected';
                    break;
            }
        }
        notifyDotNetRefs(newState);
    }

    function startMonitoring() {
        if (monitorInterval) {
            clearInterval(monitorInterval);
        }
        console.log("Starting connection monitoring...");
        // Check frequently
        monitorInterval = setInterval(checkConnection, 1000);
        checkConnection(); // Initial check
    }

    function initializeMonitoring() {
        // Wait for Blazor to potentially be ready
        if (!window.Blazor || !window.Blazor._internal) {
            // console.log("Blazor not ready, retrying monitor init..."); // Optional log
            setTimeout(initializeMonitoring, 200);
            return;
        }
        // Even if Blazor object exists, circuit might take time
        if (!getSignalRConnection()) {
             // console.log("Blazor circuit not ready, retrying monitor init..."); // Optional log
            setTimeout(initializeMonitoring, 500);
            return;
        }

        console.log("Blazor detected, initializing connection monitoring.");
        startMonitoring();
    }

    // --- Exports ---

    // Function for other JS modules to check state
    window.isSignalRConnected = function() {
        // Check our tracked state first for efficiency
        if (connectionState === "Connected") return true;
        // As a fallback, check the live connection if state is unknown/disconnected
        const connection = getSignalRConnection();
        return !!(connection && connection.connectionState === "Connected");
    };

    // Function for .NET components to register for updates
    window.registerConnectionMonitor = function(dotNetRef) {
        if (!dotNetRef) return null;
        dotNetRefs.add(dotNetRef);
        console.log("Connection monitor: Added .NET callback reference.");
        // Return current state immediately
        return connectionState;
    };

    // Function for .NET components to unregister
    window.unregisterConnectionMonitor = function(dotNetRef) {
         if (!dotNetRef) return;
         dotNetRefs.delete(dotNetRef);
         console.log("Connection monitor: Removed .NET callback reference.");
    };

    // Start the whole process
    // Use DOMContentLoaded as a reliable starting point
    document.addEventListener('DOMContentLoaded', initializeMonitoring);

})();