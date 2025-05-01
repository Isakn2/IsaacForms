using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Npgsql; 
using Microsoft.EntityFrameworkCore; 
using CustomFormsApp.Data; 
using CustomFormsApp.Data.Models; 

namespace CustomFormsApp.Services;

public class ClerkUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
    public string? Username { get; set; }
}

public class ClerkAuthService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ClerkAuthService> _logger;
    private readonly IConfiguration _configuration;
    private DotNetObjectReference<ClerkAuthService>? _dotNetRef;
    private readonly IServiceProvider _serviceProvider;
    private SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
    private TaskCompletionSource<bool> _initializationTask = new TaskCompletionSource<bool>();
    
    private ClerkUserDto? _currentUser;
    private bool _initialized = false;
    private string _connectionState = "Unknown";
    private const int DEFAULT_RETRY_ATTEMPTS = 3;
    private const int DEFAULT_RETRY_DELAY_MS = 500;
    public bool IsInitializing { get; private set; }

    public event Action<ClerkUserDto>? UserSignedIn;
    public event Action? UserSignedOut;
    public event Action<Exception>? AuthError;

    public bool IsAuthenticated => _currentUser != null;
    public ClerkUserDto? CurrentUser => _currentUser;
    public bool IsInitialized => _initialized;
    public IClerkApiService ClerkApiService { get; }

    public ClerkAuthService(
        IJSRuntime jsRuntime,
        NavigationManager navigationManager,
        ILogger<ClerkAuthService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IClerkApiService clerkApiService)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        ClerkApiService = clerkApiService;
    }

    public async Task InitializeAsync(string publishableKey)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(); 
        try
        {
            if (_initialized) return; 

            IsInitializing = true;
            _logger.LogInformation("Starting ClerkAuthService initialization..."); 

            _dotNetRef ??= DotNetObjectReference.Create(this);
            
            // *** Replace waitForClerk with initializeAndLoadClerk ***
            _logger.LogInformation("Calling clerkInterop.initializeAndLoadClerk..."); 
            // This JS function calls Clerk.load() internally
            bool loadSuccess = await _jsRuntime.InvokeAsync<bool>("clerkInterop.initializeAndLoadClerk", publishableKey, _dotNetRef); 
            
            if (!loadSuccess)
            {
                 _logger.LogError("clerkInterop.initializeAndLoadClerk returned false.");
                 // Set error state and exit
                 var ex = new InvalidOperationException("Clerk.load() failed in JavaScript.");
                 _initializationTask.TrySetException(ex); 
                 AuthError?.Invoke(ex); 
                 return; // Stop initialization here
            }
            _logger.LogInformation("clerkInterop.initializeAndLoadClerk completed successfully."); 

            // Registering handlers might be redundant if initializeAndLoadClerk does it, 
            // but let's keep it for now as it handles retries.
            _logger.LogInformation("Registering Clerk event handlers...");
            await _jsRuntime.InvokeVoidAsync("clerkInterop.registerClerkEventHandlers", _dotNetRef);
            _logger.LogInformation("Clerk event handlers registered.");

            _logger.LogInformation("Getting current Clerk user...");
            var user = await _jsRuntime.InvokeAsync<ClerkUserDto?>("clerkInterop.getCurrentUser");
            _logger.LogInformation("Current user check completed. User found: {UserFound}", user != null);
            if (user != null)
            {
                _currentUser = user;
                await OnUserSignedIn(user); 
            }

            _initialized = true;
            _initializationTask.TrySetResult(true); 
            _logger.LogInformation("ClerkAuthService initialized successfully.");
        }
        catch (JSException ex) 
        {
            _logger.LogError(ex, "Clerk initialization failed due to JSException.");
            _initializationTask.TrySetException(ex); 
            AuthError?.Invoke(ex); 
        }
        catch (TaskCanceledException ex) 
        {
            _logger.LogError(ex, "Clerk initialization failed due to TaskCanceledException (Timeout).");
            _initializationTask.TrySetException(ex); 
            AuthError?.Invoke(ex); 
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Clerk initialization failed due to unexpected error.");
            _initializationTask.TrySetException(ex); 
            AuthError?.Invoke(ex); 
        }
        finally
        {
            IsInitializing = false;
            _initLock.Release(); 
            _logger.LogInformation("ClerkAuthService initialization attempt finished."); 
        }
    }
    
    private async Task RegisterClerkEvents()
    {
        if (_dotNetRef == null)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
        }

        // Ensure this matches the function name in clerkInterop.js
        // Example: "clerkInterop.registerClerkEventHandlers"
        var success = await _jsRuntime.InvokeAsync<bool>("clerkInterop.registerClerkEventHandlers", _dotNetRef);

        if (!success)
        {
            _logger.LogWarning("Failed to register Clerk event handlers via JS Interop.");
        }
        else
        {
            _logger.LogInformation("Clerk event handlers registered successfully via JS Interop.");
        }
    }

    public async Task OpenSignIn(string? redirectUrl = null)
    {
        try
        {
            await WaitForInitializationAsync(); // Ensure initialization completed

            var actualRedirectUrl = redirectUrl ?? _navigationManager.Uri;
            // Assumes clerkInterop.js has: window.clerkInterop.openSignIn = (url) => window.Clerk?.openSignIn({ redirectUrl: url });
            await _jsRuntime.InvokeVoidAsync("clerkInterop.openSignIn", actualRedirectUrl);
        }
        catch (JSDisconnectedException ex) {
            _logger.LogWarning(ex, "Cannot open sign-in: Connection disconnected.");
            AuthError?.Invoke(new InvalidOperationException("Connection lost. Please refresh.", ex));
        }
        catch (JSException ex) {
            _logger.LogError(ex, "JavaScript error opening Clerk sign-in.");
            AuthError?.Invoke(ex);
        }
        catch (Exception ex) { // Catch other errors like timeout from WaitForInitializationAsync
            _logger.LogError(ex, "Error opening Clerk sign-in.");
            AuthError?.Invoke(ex);
        }
    }

    public async Task OpenSignUp(string? redirectUrl = null)
    {
        try
        {
            await WaitForInitializationAsync();

            var actualRedirectUrl = redirectUrl ?? _navigationManager.Uri;
            // Assumes clerkInterop.js has: window.clerkInterop.openSignUp = (url) => window.Clerk?.openSignUp({ redirectUrl: url });
            await _jsRuntime.InvokeVoidAsync("clerkInterop.openSignUp", actualRedirectUrl);
        }
        catch (JSDisconnectedException ex) {
            _logger.LogWarning(ex, "Cannot open sign-up: Connection disconnected.");
            AuthError?.Invoke(new InvalidOperationException("Connection lost. Please refresh.", ex));
        }
        catch (JSException ex) {
            _logger.LogError(ex, "JavaScript error opening Clerk sign-up.");
            AuthError?.Invoke(ex);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error opening Clerk sign-up.");
            AuthError?.Invoke(ex);
        }
    }

    public async Task<bool> SignOutAsync()
    {
        try
        {
            await WaitForInitializationAsync();

            // Assumes clerkInterop.js has: window.clerkInterop.signOut = () => window.Clerk?.signOut();
            await _jsRuntime.InvokeVoidAsync("clerkInterop.signOut");

            // Note: The actual sign-out state update (_currentUser = null) should happen
            // in the OnUserSignedOut method, triggered by the Clerk event listener.
            // Avoid setting _currentUser = null directly here to prevent race conditions.
            return true;
        }
        catch (JSDisconnectedException ex) {
            _logger.LogWarning(ex, "Cannot sign out: Connection disconnected.");
            AuthError?.Invoke(new InvalidOperationException("Connection lost. Please refresh.", ex));
            return false;
        }
        catch (JSException ex) {
            _logger.LogError(ex, "JavaScript error signing out with Clerk.");
            AuthError?.Invoke(ex);
            return false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error signing out with Clerk.");
            AuthError?.Invoke(ex);
            return false;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            await WaitForInitializationAsync();

            // Assumes clerkInterop.js has: window.clerkInterop.getToken = async () => await window.Clerk?.session?.getToken();
            return await _jsRuntime.InvokeAsync<string?>("clerkInterop.getToken");
        }
        catch (JSDisconnectedException ex) {
            _logger.LogWarning(ex, "Cannot get token: Connection disconnected.");
            AuthError?.Invoke(new InvalidOperationException("Connection lost. Please refresh.", ex));
            return null;
        }
        catch (JSException ex) {
            _logger.LogError(ex, "JavaScript error getting Clerk token.");
            AuthError?.Invoke(ex);
            return null;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting Clerk token.");
            AuthError?.Invoke(ex);
            return null;
        }
    }

    private bool _userSynchronizationInProgress = false;
    private string? _lastSignedInUserId = null;
    private readonly TimeSpan _minimumTimeBetweenSyncs = TimeSpan.FromMinutes(5); // Increased from seconds to minutes
    private DateTime _lastUserSyncTime = DateTime.MinValue;

    [JSInvokable]
    public async Task OnUserSignedIn(ClerkUserDto user)
    {
        // Skip if we're already processing this user's sign-in or synced recently
        if (user?.Id == null)
        {
            _logger.LogWarning("Received sign-in event with null user or null user.Id");
            return;
        }

        // If it's the same user ID and we've synced recently, don't trigger another sync
        bool isSameUser = user.Id == _lastSignedInUserId;
        bool recentlySynced = (DateTime.UtcNow - _lastUserSyncTime) < _minimumTimeBetweenSyncs;
        
        if (isSameUser && recentlySynced)
        {
            _logger.LogDebug("Ignoring duplicate sign-in event for user {UserId} (synced {Seconds}s ago)", 
                user.Id, (DateTime.UtcNow - _lastUserSyncTime).TotalSeconds);
            return;
        }

        // Skip if we're already in the process of syncing this user
        if (_userSynchronizationInProgress && isSameUser) 
        {
            _logger.LogDebug("Skipping duplicate sync for user {UserId} - sync already in progress", user.Id);
            return;
        }

        _currentUser = user;
        _lastSignedInUserId = user.Id;
        _logger.LogInformation("User signed in: {UserId}", user.Id);

        try
        {
            _userSynchronizationInProgress = true;
            await SyncUserWithDatabaseAsync(user);
            _lastUserSyncTime = DateTime.UtcNow;
            
            // Only notify subscribers if authentication state actually changed
            UserSignedIn?.Invoke(user);
        }
        finally
        {
            _userSynchronizationInProgress = false;
        }
    }

    private async Task SyncUserWithDatabaseAsync(ClerkUserDto clerkUser)
    {
        const int maxRetries = 3;
        const int initialRetryDelayMs = 500;
        int attempt = 0;
        
        if (clerkUser?.Id == null)
        {
            _logger.LogWarning("Cannot sync null user or user with null ID");
            return;
        }

        while (attempt < maxRetries)
        {
            attempt++;
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // First check if the user already exists outside of transaction
                var userExists = await dbContext.Users.AnyAsync(u => u.Id == clerkUser.Id);
                
                // Use the execution strategy properly instead of manually creating a transaction
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    // Begin transaction within execution strategy
                    await using var transaction = await dbContext.Database.BeginTransactionAsync();
                    try
                    {
                        ClerkUserDbModel? dbUser;
                        
                        if (!userExists)
                        {
                            // Double-check inside transaction for extra safety
                            dbUser = await dbContext.Users
                                .FirstOrDefaultAsync(u => u.Id == clerkUser.Id);
                            
                            if (dbUser == null)
                            {
                                // Create new user
                                dbUser = new ClerkUserDbModel
                                {
                                    Id = clerkUser.Id,
                                    FirstName = clerkUser.FirstName,
                                    LastName = clerkUser.LastName,
                                    Email = clerkUser.Email ?? string.Empty,
                                    ImageUrl = clerkUser.ImageUrl,
                                    Username = clerkUser.Username ?? GenerateDefaultUsername(clerkUser),
                                    DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim(),
                                    CreatedAt = DateTime.UtcNow,
                                    LastLoginAt = DateTime.UtcNow
                                };
                                
                                dbContext.Users.Add(dbUser);
                                _logger.LogInformation("Creating new user record for {UserId}", clerkUser.Id);
                            }
                            else
                            {
                                // User was created between our first check and transaction - just update
                                UpdateUserProperties(dbUser, clerkUser);
                                _logger.LogInformation("Updating user that was just created: {UserId}", clerkUser.Id);
                            }
                        }
                        else
                        {
                            // Update existing user
                            dbUser = await dbContext.Users
                                .FirstOrDefaultAsync(u => u.Id == clerkUser.Id);
                            
                            if (dbUser != null)
                            {
                                UpdateUserProperties(dbUser, clerkUser);
                                _logger.LogInformation("Updating existing user {UserId}", clerkUser.Id);
                            }
                            else
                            {
                                // This should not happen, but handle it just in case
                                _logger.LogWarning("User {UserId} existed in first check but not in second - possible race condition", clerkUser.Id);
                                await transaction.RollbackAsync();
                                return; // Exit the execution strategy lambda
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation("Successfully synced user {UserId} (Attempt {Attempt})", 
                            clerkUser.Id, attempt);
                    }
                    catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                    {
                        // Handle the specific case of duplicate key constraint violation
                        _logger.LogWarning(ex, "Duplicate key detected while syncing user {UserId} (Attempt {Attempt})", 
                            clerkUser.Id, attempt);
                            
                        await transaction.RollbackAsync();
                        throw; // Let the execution strategy handle retry
                    }
                    catch (Exception)
                    {
                        // Ensure transaction is rolled back on any exception
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
                
                // If we got here, the operation was successful
                return;
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex) && attempt == maxRetries)
            {
                // On last attempt with duplicate key, try a different approach - just update the user
                _logger.LogWarning(ex, "Final attempt: trying direct update for user {UserId}", clerkUser.Id);
                await UpdateExistingUserWithoutInsert(dbContext, clerkUser);
                return;
            }
            catch (NpgsqlException ex) when (IsTransient(ex))
            {
                _logger.LogWarning(ex, 
                    "Transient database error syncing user {UserId} (Attempt {Attempt})", 
                    clerkUser.Id, attempt);
                    
                if (attempt < maxRetries)
                {
                    var delay = initialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                    continue;
                }
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, 
                    "Concurrency conflict while syncing user {UserId}", 
                    clerkUser.Id);
                    
                if (attempt < maxRetries)
                {
                    var delay = initialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                    continue;
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error syncing user {UserId}", 
                    clerkUser.Id);
                    
                throw; // Rethrow after logging
            }
        }
        
        _logger.LogError("Failed to sync user {UserId} after {MaxRetries} attempts", 
            clerkUser.Id, maxRetries);
        throw new InvalidOperationException(
            $"Failed to sync user after {maxRetries} attempts");
    }

    // Helper method to update user properties
    private void UpdateUserProperties(ClerkUserDbModel dbUser, ClerkUserDto clerkUser)
    {
        dbUser.FirstName = clerkUser.FirstName ?? dbUser.FirstName;
        dbUser.LastName = clerkUser.LastName ?? dbUser.LastName;
        dbUser.Email = clerkUser.Email ?? dbUser.Email;
        dbUser.ImageUrl = clerkUser.ImageUrl ?? dbUser.ImageUrl;
        dbUser.Username = clerkUser.Username ?? dbUser.Username;
        dbUser.LastLoginAt = DateTime.UtcNow;
    }

    // As a last resort, explicitly update an existing user
    private async Task UpdateExistingUserWithoutInsert(ApplicationDbContext dbContext, ClerkUserDto clerkUser)
    {
        try
        {
            // Use SQL to update directly, avoiding the insert completely
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            
            var existingUser = await dbContext.Users
                .AsNoTracking() // Don't track to avoid conflicts
                .FirstOrDefaultAsync(u => u.Id == clerkUser.Id);
                
            if (existingUser != null)
            {
                // User exists, do a direct update with tracked entity
                var user = await dbContext.Users.FindAsync(clerkUser.Id);
                if (user != null) // Add null check
                {
                    UpdateUserProperties(user, clerkUser);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Successfully updated existing user {UserId} as fallback", clerkUser.Id);
                }
                else
                {
                    _logger.LogWarning("User {UserId} couldn't be found with FindAsync despite existing in query", clerkUser.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback user update for {UserId}", clerkUser.Id);
            // Don't throw - this is our last attempt fallback
        }
    }

    // Helper to check if exception is transient
    private bool IsTransient(NpgsqlException ex)
    {
        // Common transient error codes in PostgreSQL
        string[] transientErrorCodes = {
            "40001", // Serialization failure
            "40P01", // Deadlock detected
            "53300", // Too many connections
            "53400", // Configuration limit exceeded
            "57P03", // Cannot connect now
            "57P04", // Database dropped
            "08006", // Connection failure
            "08001", // SQL client unable to establish SQL connection
            "08004", // Rejected SQL connection
            "XX000"  // Internal error
        };
        
        return transientErrorCodes.Contains(ex.SqlState);
    }

    // Helper to check for duplicate key violations
    private bool IsDuplicateKeyException(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState == "23505"; // Unique violation code
        }
        return false;
    }

private string GenerateDefaultUsername(ClerkUserDto user)
{
    var baseName = $"{user.FirstName?.ToLower() ?? "user"}_{user.LastName?.ToLower() ?? "anon"}";
    var randomSuffix = new Random().Next(1000, 9999);
    return $"{baseName}_{randomSuffix}";
}

    [JSInvokable]
    public void OnUserSignedOut()
    {
        var oldUser = _currentUser;
        _currentUser = null;
        _logger.LogInformation("User signed out: {UserId}", oldUser?.Id ?? "unknown");
        UserSignedOut?.Invoke();
    }

    [JSInvokable]
    public void OnConnectionChange(string state, string? detail = null)
    {
        _connectionState = state;
        _logger.LogInformation("Connection state changed to: {State} {Detail}", 
            state, detail ?? string.Empty);
    }
    
    // Add method to wait for initialization
    public Task WaitForInitializationAsync()
    {
        return _initialized ? Task.CompletedTask : _initializationTask.Task;
    }

    public async Task<bool> IsClerkReadyAsync()
    {
        try
        {
            // Rely on the initialization flag first for a quick check
            if (!_initialized)
            {
                _logger.LogDebug("IsClerkReadyAsync check: Not initialized yet.");
                return false;
            }
            // Then verify with JS interop using the specific function
            // Assumes clerkInterop.js has: window.clerkInterop.isClerkReady = () => !!(window.Clerk && typeof window.Clerk.isReady === 'function' && window.Clerk.isReady());
            return await _jsRuntime.InvokeAsync<bool>("clerkInterop.isClerkReady");
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "JS interop disconnected checking Clerk readiness.");
            return false; // Cannot be ready if disconnected
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "JS interop error checking Clerk readiness.");
            return false;
        }
        catch (Exception ex) // Catch potential TaskCanceled or other issues
        {
            _logger.LogError(ex, "Unexpected error checking Clerk readiness.");
            return false;
        }
    }

    public async Task<bool> VerifyClerkConfiguration()
    {
        try
        {
            await WaitForInitializationAsync();

            // Check if Clerk JS client reports it's ready
            var isReady = await IsClerkReadyAsync();
            if (!isReady)
            {
                _logger.LogWarning("Cannot verify configuration: Clerk JS client reports it's not ready.");
                return false;
            }

            var token = await GetTokenAsync();
            if (token == null)
            {
                _logger.LogInformation("VerifyClerkConfiguration: Could not obtain Clerk token (user might be signed out or config issue).");
            }

            // Log successful verification (based on readiness)
            _logger.LogInformation("Clerk configuration verified successfully (Clerk JS is ready).");
            return true;
        }
        catch (JSDisconnectedException ex)
        {
            _logger.LogWarning(ex, "Cannot verify configuration: JS Interop disconnected.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Clerk configuration");
            return false;
        }
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _initLock.Dispose();
    }

}