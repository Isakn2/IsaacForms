using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AdminService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Expose the DbContextFactory through the interface
        public IDbContextFactory<ApplicationDbContext> DbContextFactory => _contextFactory;

        public AdminService(
            IDbContextFactory<ApplicationDbContext> contextFactory, 
            ILogger<AdminService> logger,
            IServiceProvider serviceProvider)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<(IEnumerable<ClerkUserDbModel> Users, int TotalCount)> GetUsersAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, pageSize);

                // For debugging purposes, log the query details
                _logger.LogInformation("Executing GetUsersAsync: Page {Page}, Size {Size}, Search '{Search}'", 
                    pageNumber, pageSize, searchTerm);

                await using var _context = _contextFactory.CreateDbContext();
                IQueryable<ClerkUserDbModel> query = _context.Users;

                // Log total number of users in database before any filtering
                var totalUsersInDb = await query.CountAsync();
                _logger.LogInformation("Total users in database (before filtering): {Count}", totalUsersInDb);

                // --- Apply Search Filter ---
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string searchPattern = $"%{searchTerm.Trim()}%";
                    query = query.Where(u => EF.Functions.Like(u.Email, searchPattern) ||
                                             (u.Username != null && EF.Functions.Like(u.Username, searchPattern)) ||
                                             (u.FirstName != null && EF.Functions.Like(u.FirstName, searchPattern)) ||
                                             (u.LastName != null && EF.Functions.Like(u.LastName, searchPattern)));
                }

                // Get total count *after* applying search filter
                int totalCount = await query.CountAsync();
                _logger.LogInformation("Total users after search filter: {Count}", totalCount);

                // Apply ordering and pagination
                var pagedQuery = query
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ThenBy(u => u.Email)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                // Execute query and materialize results
                var users = await pagedQuery.AsNoTracking().ToListAsync();
                _logger.LogInformation("Retrieved {Count} users for page {Page}", users.Count, pageNumber);

                // Log the first few user IDs for debugging
                if (users.Any())
                {
                    var firstFewUsers = string.Join(", ", users.Take(3).Select(u => $"{u.Id}: {u.Email}"));
                    _logger.LogInformation("First few users: {Users}", firstFewUsers);
                }
                else
                {
                    _logger.LogWarning("No users found in the query result");
                }

                return (users, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated/searched users for admin (Page: {Page}, Size: {Size}, Search: '{Search}').",
                    pageNumber, pageSize, searchTerm);
                return (Enumerable.Empty<ClerkUserDbModel>(), 0);
            }
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Counts all users present in the local Users table
                return await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total user count.");
                return 0; // Return 0 on error
            }
        }

        public async Task<int> GetTotalTemplateCountAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Explicitly ignore query filters to see all templates, including deleted ones
                return await _context.Templates.IgnoreQueryFilters().CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total template count: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<int> GetTotalResponseCountAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Explicitly ignore query filters to see all responses
                return await _context.FormResponses.IgnoreQueryFilters().CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total response count: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<bool> DeleteTemplateAsAdminAsync(int templateId)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Find the template, ignoring the default IsDeleted filter
                var template = await _context.Templates
                                        .IgnoreQueryFilters()
                                        .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning("Admin delete failed: Template {TemplateId} not found.", templateId);
                    return false;
                }

                if (template.IsDeleted)
                {
                    _logger.LogInformation("Admin delete skipped: Template {TemplateId} is already deleted.", templateId);
                    return true; // Already deleted, consider it success
                }

                template.IsDeleted = true;
                template.LastModifiedDate = DateTime.UtcNow;
                _context.Templates.Update(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template {TemplateId} soft-deleted by admin.", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId} as admin.", templateId);
                return false;
            }
        }

        public async Task<bool> RestoreTemplateAsync(int templateId)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Find the template, explicitly including deleted ones
                var template = await _context.Templates
                                        .IgnoreQueryFilters() // Important to find deleted templates
                                        .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning("Admin restore failed: Template {TemplateId} not found.", templateId);
                    return false;
                }

                if (!template.IsDeleted)
                {
                    _logger.LogInformation("Admin restore skipped: Template {TemplateId} is not deleted.", templateId);
                    return true; // Already active, consider it success
                }

                template.IsDeleted = false;
                template.LastModifiedDate = DateTime.UtcNow;
                _context.Templates.Update(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template {TemplateId} restored by admin.", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring template {TemplateId} as admin.", templateId);
                return false;
            }
        }

        public async Task<bool> DeleteResponseAsAdminAsync(int responseId)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                // Find the response, ignoring query filters to ensure it can be found even if linked to a deleted template
                var response = await _context.FormResponses
                                        .IgnoreQueryFilters() // Important to find the response regardless of template status
                                        // .Include(r => r.Answers) // Include Answers if Cascade delete isn't configured or for logging
                                        .FirstOrDefaultAsync(r => r.Id == responseId);

                if (response == null)
                {
                    _logger.LogWarning("Admin delete failed: Response {ResponseId} not found.", responseId);
                    return false;
                }

                // Cascade delete should handle removing associated Answers if configured correctly in OnModelCreating
                _context.FormResponses.Remove(response);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Response {ResponseId} deleted by admin.", responseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting response {ResponseId} as admin.", responseId);
                return false;
            }
        }

        public async Task<(IEnumerable<FormResponse> Responses, int TotalCount)> GetAllResponsesAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, pageSize);

                await using var _context = _contextFactory.CreateDbContext();
                // Base query, ignoring filters to see all responses regardless of template status
                IQueryable<FormResponse> query = _context.FormResponses.IgnoreQueryFilters();

                // --- Apply Search Filter (Submitter) ---
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string searchPattern = $"%{searchTerm.Trim()}%";
                    // Filter based on related SubmittedBy user's properties
                    query = query.Where(r => r.SubmittedBy != null &&
                                             (EF.Functions.Like(r.SubmittedBy.Email, searchPattern) ||
                                              (r.SubmittedBy.Username != null && EF.Functions.Like(r.SubmittedBy.Username, searchPattern))));
                }

                // Get total count *after* applying search filter
                int totalCount = await query.CountAsync();

                // Apply includes, ordering, and pagination
                var pagedQuery = query
                    .Include(r => r.SubmittedBy) // Ensure SubmittedBy is included for filtering and display
                    .Include(r => r.Form)
                        .ThenInclude(f => f!.Template) // Include Template via Form
                    .OrderByDescending(r => r.SubmissionDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                var responses = await pagedQuery.AsNoTracking().ToListAsync();

                return (responses, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated/searched responses for admin (Page: {Page}, Size: {Size}, Search: '{Search}').",
                    pageNumber, pageSize, searchTerm);
                return (Enumerable.Empty<FormResponse>(), 0);
            }
        }

        public async Task<(IEnumerable<Template> Templates, int TotalCount)> GetAllTemplatesAsync(
            int pageNumber,
            int pageSize,
            bool includeDeleted = false,
            string? templateSearchTerm = null) 
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, pageSize);

                await using var _context = _contextFactory.CreateDbContext();
                IQueryable<Template> query = _context.Templates;

                if (includeDeleted)
                {
                    query = query.IgnoreQueryFilters();
                }

                // --- Apply Search Filter ---
                if (!string.IsNullOrWhiteSpace(templateSearchTerm)) // Use templateSearchTerm here
                {
                    string searchPattern = $"%{templateSearchTerm.Trim()}%"; // Use templateSearchTerm here
                    query = query.Where(t => EF.Functions.Like(t.Title, searchPattern) ||
                                            (t.Description != null && EF.Functions.Like(t.Description, searchPattern)));
                }

                // Get total count *after* applying search filter
                int totalCount = await query.CountAsync();

                // Apply ordering and pagination
                var pagedQuery = query
                    .Include(t => t.CreatedBy) // Include creator info
                    .OrderByDescending(t => t.LastModifiedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                var templates = await pagedQuery.AsNoTracking().ToListAsync();

                return (templates, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated/searched templates for admin (Page: {Page}, Size: {Size}, Search: '{Search}').",
                    pageNumber, pageSize, templateSearchTerm); // Use templateSearchTerm in log
                return (Enumerable.Empty<Template>(), 0);
            }
        }

        public async Task<bool> SetUserAdminRoleAsync(string userId, bool isAdmin)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Admin role change failed: User {UserId} not found.", userId);
                    return false;
                }

                user.IsAdmin = isAdmin;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                if (isAdmin)
                {
                    _logger.LogInformation("User {UserId} was granted admin role.", userId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} had admin role removed.", userId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing admin role for user {UserId} to {IsAdmin}.", userId, isAdmin);
                return false;
            }
        }

        public async Task<bool> BlockUserAsync(string userId, bool blocked)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User block/unblock failed: User {UserId} not found.", userId);
                    return false;
                }

                user.IsBlocked = blocked;
                
                if (blocked)
                {
                    user.BlockedAt = DateTime.UtcNow;
                }
                else
                {
                    user.BlockedAt = null;
                    user.BlockedReason = null;
                }
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                if (blocked)
                {
                    _logger.LogInformation("User {UserId} was blocked.", userId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} was unblocked.", userId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error {Action} user {UserId}.", blocked ? "blocking" : "unblocking", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User delete failed: User {UserId} not found.", userId);
                    return false;
                }

                // Remove the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {UserId} was deleted.", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", userId);
                return false;
            }
        }

        public async Task<string> GetDatabaseDiagnosticsAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userCount = await _context.Users.CountAsync();
                var templateCount = await _context.Templates.IgnoreQueryFilters().CountAsync();
                var responseCount = await _context.FormResponses.IgnoreQueryFilters().CountAsync();
                var formCount = 0;
                
                try
                {
                    formCount = await _context.Forms.IgnoreQueryFilters().CountAsync();
                }
                catch (Exception formEx)
                {
                    _logger.LogError(formEx, "Error counting forms");
                }
                
                return $"Users: {userCount}, Templates: {templateCount}, Forms: {formCount}, Responses: {responseCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database diagnostic error: {Message}", ex.Message);
                return $"Error querying database: {ex.Message}";
            }
        }

        public async Task<bool> SyncUsersFromClerkAsync()
        {
            try
            {
                _logger.LogInformation("Starting sync of all users from Clerk API");
                
                // Get the ClerkUserService from the service provider
                // This is a bit of a workaround since we don't directly inject it
                var clerkUserService = _serviceProvider.GetRequiredService<ClerkUserService>();
                
                // Call the sync method
                var syncedUsers = await clerkUserService.SyncAllUsersAsync();
                
                if (syncedUsers == null || !syncedUsers.Any())
                {
                    _logger.LogWarning("No users were synced from Clerk API");
                    return false;
                }
                
                _logger.LogInformation("Successfully synced {Count} users from Clerk API", syncedUsers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing users from Clerk API: {Message}", ex.Message);
                return false;
            }
        }
    }
}