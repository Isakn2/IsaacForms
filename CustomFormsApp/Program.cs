using Microsoft.AspNetCore.Components.Authorization;
using CustomFormsApp.Services;
using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using CustomFormsApp.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Plk.Blazor.DragDrop;
using CustomFormsApp.Components.Account;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity Configuration with Roles (required for admin functionality)
// MODIFIED: Combined Identity and Authentication configuration to avoid duplicate schemes
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    // Simplified password requirements for development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI() // Added to handle Identity UI pages
.AddUserManager<UserManager<ApplicationUser>>()
.AddRoleManager<RoleManager<IdentityRole>>()
.AddSignInManager<SignInManager<ApplicationUser>>(); // Added for better auth flow

// REMOVED: The separate AddAuthentication block that was causing the conflict

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("TemplateOwnerPolicy", policy =>
        policy.Requirements.Add(new TemplateOwnerRequirement()));
});

// Register custom services
builder.Services.AddScoped<IAuthorizationHandler, TemplateOwnerAuthorizationHandler>();
builder.Services.AddScoped<AuthenticationStateProvider, 
    RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
builder.Services.AddCascadingAuthenticationState();

// Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    
    // Add these handlers to prevent redirect errors
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["Accept"].Contains("application/json"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["Accept"].Contains("application/json"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Application Services
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICloudStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<ThemeService>();

// HttpClient Configuration
builder.Services.AddHttpClient("ServerAPI", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["BaseAddress"] ?? builder.Environment.WebRootPath);
});

// Blazor and UI Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options => options.DetailedErrors = true);
builder.Services.AddBlazorDragDrop(); // For question reordering
builder.Services.AddHttpContextAccessor();

// Full-text search configuration
builder.Services.AddScoped<IFullTextSearchService, SqlFullTextSearchService>();

var app = builder.Build();
// Set service provider for ThemeService static methods
ThemeService.SetServiceProvider(app.Services.CreateScope().ServiceProvider);

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(); // For multilingual support

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Seed admin role and initial admin user
await SeedAdminUserAndRoles(app);

app.Run();

async Task SeedAdminUserAndRoles(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Create Admin role if not exists
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    // Create initial admin user if none exists
    var adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser 
        { 
            UserName = adminEmail, 
            Email = adminEmail,
            EmailConfirmed = true,
            CreatorId = null  // Explicitly set to null for the first user
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError("Failed to create admin user: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}