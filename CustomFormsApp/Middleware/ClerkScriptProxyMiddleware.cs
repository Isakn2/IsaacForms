using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System;

namespace CustomFormsApp.Middleware
{
    public class ClerkScriptProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly ILogger<ClerkScriptProxyMiddleware> _logger;

        public ClerkScriptProxyMiddleware(
            RequestDelegate next,
            IConfiguration config,
            ILogger<ClerkScriptProxyMiddleware> logger)
        {
            _next = next;
            _config = config;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/_clerk-proxy"))
            {
                try
                {
                    var frontendApi = _config["Clerk:ApiUrl"]?.TrimEnd('/');
                    var publishableKey = _config["Clerk:PublishableKey"];
                    
                    if (string.IsNullOrEmpty(frontendApi) || string.IsNullOrEmpty(publishableKey))
                    {
                        _logger.LogError("Missing Clerk configuration - FrontendApi: {FrontendApi}, PublishableKey: {PublishableKey}", 
                            frontendApi, string.IsNullOrEmpty(publishableKey) ? "MISSING" : "PRESENT");
                        
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Missing Clerk configuration");
                        return;
                    }
                    
                    var script = $@"
                        window.__clerk_publishable_key = '{publishableKey}';
                        window.__clerk_frontend_api = 'https://{frontendApi}';
                        
                        (() => {{
                            const script = document.createElement('script');
                            // Use the clerk-js@5 version specifically as in your documentation
                            script.src = 'https://meet-phoenix-78.clerk.accounts.dev/npm/@clerk/clerk-js@5/dist/clerk.browser.js';
                            script.async = true;
                            script.crossOrigin = 'anonymous';
                            script.onload = () => window.dispatchEvent(new Event('clerk-loaded'));
                            script.onerror = (e) => console.error('Clerk script failed to load', e);
                            document.head.appendChild(script);
                        }})();
                    ";
                    
                    context.Response.ContentType = "application/javascript";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(script));
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating Clerk proxy script");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error loading Clerk script");
                    return;
                }
            }

            await _next(context);
        }
    }
}