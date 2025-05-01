using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Services;

public class ClerkUserService
{
    // Inject the factory instead of the context
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ClerkUserService> _logger;
    private readonly ClerkAuthService _authService;

    public ClerkUserService(
        // Update constructor signature
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ClerkUserService> logger,
        ClerkAuthService authService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _authService = authService;

        _authService.UserSignedIn += SyncUserOnSignIn;
    }
    
    private async void SyncUserOnSignIn(ClerkUserDto clerkUser)
    {
        try
        {
            await SyncUserAsync(clerkUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user on sign in: {ClerkUserId}", clerkUser.Id);
        }
    }
    
    public async Task<ClerkUserDbModel> SyncUserAsync(ClerkUserDto clerkUser)
    {
        // Create a context instance for this operation
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        try
        {
            var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == clerkUser.Id);

            if (dbUser == null)
            {
                dbUser = new ClerkUserDbModel
                {
                    Id = clerkUser.Id,
                    FirstName = clerkUser.FirstName,
                    LastName = clerkUser.LastName,
                    Email = clerkUser.Email, // Ensure Email is populated in ClerkUserDto
                    ImageUrl = clerkUser.ImageUrl,
                    Username = clerkUser.Username, // Ensure Username is populated
                    DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim(),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow // Set LastLoginAt on creation too
                };
                dbContext.Users.Add(dbUser);
                _logger.LogInformation("Created new user in database: {ClerkUserId}", clerkUser.Id);
            }
            else
            {
                // Update existing user
                dbUser.FirstName = clerkUser.FirstName;
                dbUser.LastName = clerkUser.LastName;
                dbUser.Email = clerkUser.Email; // Update email if changed
                dbUser.ImageUrl = clerkUser.ImageUrl;
                dbUser.Username = clerkUser.Username; // Update username if changed
                dbUser.DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim();
                dbUser.LastLoginAt = DateTime.UtcNow;

                dbContext.Users.Update(dbUser);
                _logger.LogInformation("Updated existing user in database: {ClerkUserId}", clerkUser.Id);
            }

            await dbContext.SaveChangesAsync();
            return dbUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user: {ClerkUserId}", clerkUser.Id);
            throw; // Re-throw the exception to indicate failure
        }
    }

    public async Task<ClerkUserDbModel?> GetCurrentUserAsync()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
        {
            return null;
        }

        // Create a context instance for this operation
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == _authService.CurrentUser.Id);
    }

    // Add a method to sync all users from Clerk
    public async Task<List<ClerkUserDbModel>> SyncAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Starting sync of all Clerk users to local database");
            
            // Create a context instance for this operation
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            
            // Get all users from Clerk API
            var clerkUsers = await _authService.ClerkApiService.GetAllUsersAsync();
            
            if (clerkUsers == null || !clerkUsers.Any())
            {
                _logger.LogWarning("No users returned from Clerk API");
                return new List<ClerkUserDbModel>();
            }
            
            _logger.LogInformation("Retrieved {Count} users from Clerk API", clerkUsers.Count);
            
            // Keep track of synced users
            var syncedUsers = new List<ClerkUserDbModel>();
            
            // Process each user
            foreach (var clerkUser in clerkUsers)
            {
                try
                {
                    // Create a new context for each user to avoid concurrency issues
                    await using var userDbContext = await _contextFactory.CreateDbContextAsync();
                    
                    var dbUser = await userDbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == clerkUser.Id);
                    
                    if (dbUser == null)
                    {
                        // Check for email duplicates before creating new user
                        string email = clerkUser.Email ?? "no-email@example.com";
                        bool emailExists = await userDbContext.Users.AnyAsync(u => u.Email == email);
                        
                        if (emailExists)
                        {
                            // Make the email unique by adding a suffix with the Clerk ID
                            string username = clerkUser.Username ?? "user";
                            int atIndex = email.IndexOf('@');
                            if (atIndex > 0)
                            {
                                email = $"{email.Substring(0, atIndex)}+{clerkUser.Id.Substring(0, 6)}{email.Substring(atIndex)}";
                                _logger.LogWarning("Modified duplicate email for user {UserId}. Original: {OriginalEmail}, Modified: {ModifiedEmail}", 
                                    clerkUser.Id, clerkUser.Email, email);
                            }
                        }
                        
                        // Create new user with the possibly modified email
                        dbUser = new ClerkUserDbModel
                        {
                            Id = clerkUser.Id,
                            FirstName = clerkUser.FirstName ?? "",
                            LastName = clerkUser.LastName ?? "",
                            Email = email,
                            ImageUrl = clerkUser.ImageUrl,
                            Username = clerkUser.Username ?? GenerateDefaultUsername(clerkUser),
                            DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim(),
                            CreatedAt = DateTime.UtcNow,
                            LastLoginAt = null // Never logged in yet
                        };
                        
                        userDbContext.Users.Add(dbUser);
                        _logger.LogInformation("Created new user in database: {ClerkUserId}", clerkUser.Id);
                    }
                    else
                    {
                        // Update existing user but be careful with email updates
                        dbUser.FirstName = clerkUser.FirstName ?? dbUser.FirstName;
                        dbUser.LastName = clerkUser.LastName ?? dbUser.LastName;
                        
                        // Only update email if it's changed and doesn't conflict
                        if (clerkUser.Email != null && clerkUser.Email != dbUser.Email)
                        {
                            bool emailExists = await userDbContext.Users
                                .Where(u => u.Id != dbUser.Id)  // Exclude current user
                                .AnyAsync(u => u.Email == clerkUser.Email);
                                
                            if (!emailExists)
                            {
                                dbUser.Email = clerkUser.Email;
                            }
                            else
                            {
                                _logger.LogWarning("Could not update email for user {UserId} because the new email already exists", clerkUser.Id);
                            }
                        }
                        
                        dbUser.ImageUrl = clerkUser.ImageUrl ?? dbUser.ImageUrl;
                        dbUser.Username = clerkUser.Username ?? dbUser.Username;
                        dbUser.DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim();
                        
                        userDbContext.Users.Update(dbUser);
                        _logger.LogInformation("Updated existing user in database: {ClerkUserId}", clerkUser.Id);
                    }
                    
                    try
                    {
                        // Save changes for this user
                        await userDbContext.SaveChangesAsync();
                        syncedUsers.Add(dbUser);
                    }
                    catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                    {
                        _logger.LogError(ex, "Duplicate key detected for user {UserId}. Will try a modified approach", clerkUser.Id);
                        
                        // Retry with a more direct approach for existing users
                        await RetryUserCreationWithUniqueEmail(clerkUser);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with next user
                    _logger.LogError(ex, "Error syncing user {UserId}", clerkUser.Id);
                }
            }
            
            _logger.LogInformation("Successfully synced {Count} users to local database", syncedUsers.Count);
            return syncedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all users from Clerk");
            return new List<ClerkUserDbModel>();
        }
    }
    
    private async Task<ClerkUserDbModel?> RetryUserCreationWithUniqueEmail(ClerkUserDto clerkUser)
    {
        try
        {
            await using var retryContext = await _contextFactory.CreateDbContextAsync();
            
            // Generate a guaranteed unique email
            string uniqueEmail = $"clerk_{clerkUser.Id}@unique.local";
            
            var newUser = new ClerkUserDbModel
            {
                Id = clerkUser.Id,
                FirstName = clerkUser.FirstName ?? "",
                LastName = clerkUser.LastName ?? "",
                Email = uniqueEmail,
                ImageUrl = clerkUser.ImageUrl,
                Username = clerkUser.Username ?? GenerateDefaultUsername(clerkUser),
                DisplayName = $"{clerkUser.FirstName} {clerkUser.LastName}".Trim(),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = null
            };
            
            retryContext.Users.Add(newUser);
            await retryContext.SaveChangesAsync();
            
            _logger.LogInformation("Created user with guaranteed unique email: {UserId}, Email: {Email}", clerkUser.Id, uniqueEmail);
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Final attempt to create user {UserId} failed", clerkUser.Id);
            return null;
        }
    }
    
    // Helper to check for duplicate key violations
    private bool IsDuplicateKeyException(DbUpdateException ex)
    {
        if (ex.InnerException is Npgsql.PostgresException pgEx)
        {
            // Check PostgreSQL error codes
            return pgEx.SqlState == "23505"; // Unique violation code
        }
        return false;
    }
    
    // Helper method to generate a default username
    private string GenerateDefaultUsername(ClerkUserDto user)
    {
        var baseName = $"{user.FirstName?.ToLower() ?? "user"}_{user.LastName?.ToLower() ?? "anon"}";
        var randomSuffix = new Random().Next(1000, 9999);
        return $"{baseName}_{randomSuffix}";
    }
}

