using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CustomFormsApp.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ClerkAuthService _clerkAuthService;
    private readonly ILogger<CurrentUserService> _logger;
    private ClaimsPrincipal? _cachedUser;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromSeconds(30);

    public CurrentUserService(
        AuthenticationStateProvider authenticationStateProvider,
        ClerkAuthService clerkAuthService,
        ILogger<CurrentUserService> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _clerkAuthService = clerkAuthService;
        _logger = logger;
        
        // Subscribe to auth state changes to invalidate cache
        _authenticationStateProvider.AuthenticationStateChanged += _ => InvalidateCache();
        _clerkAuthService.UserSignedIn += _ => InvalidateCache();
        _clerkAuthService.UserSignedOut += InvalidateCache;
    }

    private void InvalidateCache()
    {
        _cachedUser = null;
        _cacheExpiry = DateTime.MinValue;
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        // Check if we have a valid cached user
        if (_cachedUser != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedUser;
        }

        // Lock to prevent multiple simultaneous auth state requests
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check in case another thread already updated the cache
            if (_cachedUser != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedUser;
            }

            // Get fresh auth state
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _cachedUser = authState.User;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);
            return _cachedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new ClaimsPrincipal(new ClaimsIdentity()); // Return unauthenticated user
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public string? GetUserId()
    {
        // First try to get from Clerk service as it's most reliable
        var clerkUser = _clerkAuthService.CurrentUser;
        if (clerkUser != null && !string.IsNullOrEmpty(clerkUser.Id))
        {
            return clerkUser.Id;
        }

        // Fallback to AuthenticationState claims - use a non-blocking approach
        try
        {
            // Use GetAwaiter().GetResult() but with a timeout to prevent deadlocks
            var user = Task.Run(async () => await GetUserAsync()).Wait(500) 
                ? Task.Run(async () => await GetUserAsync()).Result 
                : new ClaimsPrincipal(new ClaimsIdentity());
            
            if (user.Identity?.IsAuthenticated == true)
            {
                // Try to find user ID in claims
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return userId;
                }
                
                // Try alternative claim types that might contain user ID
                userId = user.FindFirst("clerk:id")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return userId;
                }
                
                userId = user.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return userId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user ID from authentication state - non-blocking");
        }

        _logger.LogWarning("Could not determine user ID - user may not be authenticated");
        return null;
    }

    public string? GetUserEmail()
    {
        // First try to get from Clerk service
        var clerkUser = _clerkAuthService.CurrentUser;
        if (clerkUser != null && !string.IsNullOrEmpty(clerkUser.Email))
        {
            return clerkUser.Email;
        }

        // Fallback to AuthenticationState claims
        try
        {
            var user = Task.Run(async () => await GetUserAsync()).Wait(500) 
                ? Task.Run(async () => await GetUserAsync()).Result 
                : new ClaimsPrincipal(new ClaimsIdentity());
            
            if (user.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.Email)?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user email from authentication state");
        }

        return null;
    }

    // Renamed from GetUserName to match the interface exactly
    public string? GetUsername()
    {
        // First try to get from Clerk service
        var clerkUser = _clerkAuthService.CurrentUser;
        if (clerkUser != null)
        {
            var firstName = clerkUser.FirstName ?? string.Empty;
            var lastName = clerkUser.LastName ?? string.Empty;
            
            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            {
                return $"{firstName} {lastName}".Trim();
            }
        }

        // Fallback to AuthenticationState claims
        try
        {
            var user = Task.Run(async () => await GetUserAsync()).Wait(500) 
                ? Task.Run(async () => await GetUserAsync()).Result 
                : new ClaimsPrincipal(new ClaimsIdentity());
            
            if (user.Identity?.IsAuthenticated == true)
            {
                // Try to find name in claims
                var name = user.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
                
                // Try given name and surname
                var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
                var lastName = user.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
                
                if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
                {
                    return $"{firstName} {lastName}".Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user name from authentication state");
        }

        return null;
    }

    // Added the missing IsAdmin method
    public bool IsAdmin()
    {
        try
        {
            // Check if the user has admin role/claims from authentication state
            var user = Task.Run(async () => await GetUserAsync()).Wait(500) 
                ? Task.Run(async () => await GetUserAsync()).Result 
                : new ClaimsPrincipal(new ClaimsIdentity());
            
            if (user.Identity?.IsAuthenticated == true)
            {
                // Check for admin role in claims
                if (user.IsInRole("Admin"))
                {
                    return true;
                }
                
                // Check for admin claim with various possible names
                var isAdmin = user.FindFirst("isAdmin")?.Value;
                if (!string.IsNullOrEmpty(isAdmin) && (isAdmin.Equals("true", StringComparison.OrdinalIgnoreCase) || isAdmin == "1"))
                {
                    return true;
                }
                
                // Check for role claim containing Admin
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
                if (roles.Any(role => role.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                // Check for any claim that might indicate admin status
                var adminClaim = user.FindFirst("role")?.Value;
                if (!string.IsNullOrEmpty(adminClaim) && adminClaim.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin status");
            return false;
        }
    }

    public bool IsAuthenticated()
    {
        if (_clerkAuthService.IsAuthenticated)
        {
            return true;
        }
        
        try
        {
            var user = Task.Run(async () => await GetUserAsync()).Wait(500) 
                ? Task.Run(async () => await GetUserAsync()).Result 
                : new ClaimsPrincipal(new ClaimsIdentity());
            return user.Identity?.IsAuthenticated == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }
}