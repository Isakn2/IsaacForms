using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Authorization;

namespace CustomFormsApp.Services;

public static class ClerkServiceExtensions
{
    public static IServiceCollection AddClerkAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClerkOptions>(configuration.GetSection("Clerk"));
        
        // Validate required settings
        var publishableKey = configuration["Clerk:PublishableKey"];
        if (string.IsNullOrEmpty(publishableKey))
            throw new InvalidOperationException("Clerk PublishableKey is not configured.");
        
        // Register services
        services.AddScoped<ClerkAuthService>();
        services.AddHttpClient<ClerkApiService>();
        services.AddScoped<IClerkApiService, ClerkApiService>(); // Register with interface
        services.AddScoped<ClerkUserService>(); // Register ClerkUserService
        services.AddScoped<AuthenticationStateProvider, ClerkAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();
        services.AddAuthorizationCore();
        
        return services;
    }
}