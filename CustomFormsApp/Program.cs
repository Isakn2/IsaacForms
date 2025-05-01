using Microsoft.AspNetCore.Components.Authorization;
using CustomFormsApp.Services;
using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Plk.Blazor.DragDrop;
using CustomFormsApp.Components.Account;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using CustomFormsApp.Middleware;
using CustomFormsApp.Extensions;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

// Process environment variable placeholders
builder.Configuration.ProcessEnvironmentVariables();

// Add CORS with Clerk domains
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://js.clerk.dev",
                "https://*.clerk.accounts.dev",
                builder.Configuration["Clerk:ApiUrl"]!
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<AuthenticationStateProvider, ClerkAuthenticationStateProvider>();

// Database Configuration - Use DbContextFactory
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            // Add connection resilience
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            // Increase command timeout to handle long-running queries
            npgsqlOptions.CommandTimeout(60);
        });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Clerk Authentication Setup
builder.Services.AddClerkAuth(builder.Configuration);

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("TemplateOwnerPolicy", policy =>
        policy.Requirements.Add(new TemplateOwnerRequirement()));
});

// Application Services
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<FormBuilderService>();
builder.Services.AddScoped<IFormBuilderService, FormBuilderService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICloudStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<IAuthorizationHandler, TemplateOwnerAuthorizationHandler>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFormResponseService, FormResponseService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<LocalizationService>();

// HttpClient Configuration
builder.Services.AddHttpClient("ServerAPI", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["BaseAddress"] ?? builder.Environment.WebRootPath);
});

// Blazor and UI Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options => options.DetailedErrors = true);
builder.Services.AddBlazorDragDrop();
builder.Services.AddHttpContextAccessor();

// Configure localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddSingleton<LocalizationHelper>();

// Full-text search configuration
builder.Services.AddScoped<IFullTextSearchService, SqlFullTextSearchService>();

var app = builder.Build();
Console.WriteLine("Application built successfully");

// Set service provider for ThemeService static methods
ThemeService.SetServiceProvider(app.Services.CreateScope().ServiceProvider);

// Ensure topics are seeded
await TopicSeeder.SeedTopicsAsync(app.Services);

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    
    // Use regular static files with caching enabled, same as production
    app.UseStaticFiles();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseStaticFiles(); // Regular static files in production
}

app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"), // Default to English
    SupportedCultures = new[]
    {
        new CultureInfo("en-US"), // English (United States)
        new CultureInfo("en"),    // English (Neutral)
        new CultureInfo("es-ES"), // Spanish (Spain)
        new CultureInfo("es"),    // Spanish (Neutral)
        // Add other languages you support
    },
    SupportedUICultures = new[]
    {
        new CultureInfo("en-US"), // English (United States)
        new CultureInfo("en"),    // English (Neutral)
        new CultureInfo("es-ES"), // Spanish (Spain)
        new CultureInfo("es"),    // Spanish (Neutral)
        // Add other languages you support
    }
});

app.UseRouting();
app.UseCors();
app.UseMiddleware<ClerkScriptProxyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Console.WriteLine("Starting application server...");
app.Run();