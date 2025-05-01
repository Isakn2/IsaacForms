using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using CustomFormsApp.Data;
using Microsoft.EntityFrameworkCore;
using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Services;

public class ClerkAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly ClerkAuthService _authService;
    private readonly ILogger<ClerkAuthenticationStateProvider> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Task<AuthenticationState>? _cachedAuthStateTask;
    private DateTime _lastAuthStateRefresh = DateTime.MinValue;
    private readonly TimeSpan _authStateCacheDuration = TimeSpan.FromSeconds(10);

    public ClerkAuthenticationStateProvider(
        ClerkAuthService authService,
        ILogger<ClerkAuthenticationStateProvider> logger,
        IServiceProvider serviceProvider)
    {
        _authService = authService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Subscribe to auth state changes
        _authService.UserSignedIn += OnUserStateChanged;
        _authService.UserSignedOut += OnUserStateChanged;
    }

    private void OnUserStateChanged(ClerkUserDto user)
    {
        _logger.LogDebug("Authentication state changed - invalidating cache");
        _lastAuthStateRefresh = DateTime.MinValue; // Invalidate cache
        _cachedAuthStateTask = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void OnUserStateChanged()
    {
        _logger.LogDebug("Authentication state changed (sign-out) - invalidating cache");
        _lastAuthStateRefresh = DateTime.MinValue; // Invalidate cache
        _cachedAuthStateTask = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Use cached auth state if available and not expired
            if (_cachedAuthStateTask != null && 
                (DateTime.UtcNow - _lastAuthStateRefresh) < _authStateCacheDuration)
            {
                return await _cachedAuthStateTask;
            }

            // Create new auth state task
            _cachedAuthStateTask = CreateAuthenticationStateAsync();
            _lastAuthStateRefresh = DateTime.UtcNow;
            return await _cachedAuthStateTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAuthenticationStateAsync");
            // Return empty state on error
            return new AuthenticationState(new ClaimsPrincipal());
        }
    }

    private async Task<AuthenticationState> CreateAuthenticationStateAsync()
    {
        try
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                _logger.LogDebug("No authenticated user found");
                return new AuthenticationState(new ClaimsPrincipal());
            }

            var user = _authService.CurrentUser;
            var claims = await BuildClaimsAsync(user);
            var identity = new ClaimsIdentity(claims, "Clerk");
            
            _logger.LogDebug("Authenticated user: {UserId}", user.Id);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating authentication state");
            return new AuthenticationState(new ClaimsPrincipal());
        }
    }

    private async Task<List<Claim>> BuildClaimsAsync(ClerkUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("auth_method", "Clerk"),
            new("picture", user.ImageUrl ?? string.Empty)
        };

        if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()));
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }
        
        // Check if user is admin in the database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            var dbUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            
            if (dbUser != null && dbUser.IsAdmin)
            {
                _logger.LogInformation("User {UserId} has admin role", user.Id);
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin status for user {UserId}", user.Id);
        }

        return claims;
    }

    public void Dispose()
    {
        _authService.UserSignedIn -= OnUserStateChanged;
        _authService.UserSignedOut -= OnUserStateChanged;
    }
}