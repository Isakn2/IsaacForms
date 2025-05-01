using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public interface IAdminService
    {
        // Add the DbContextFactory property
        IDbContextFactory<ApplicationDbContext> DbContextFactory { get; }
        
        Task<int> GetTotalUserCountAsync();
        Task<int> GetTotalTemplateCountAsync();
        Task<int> GetTotalResponseCountAsync();
        Task<(IEnumerable<Template> Templates, int TotalCount)> GetAllTemplatesAsync(
            int pageNumber,
            int pageSize,
            bool includeDeleted = false,
            string? templateSearchTerm = null);
        Task<bool> DeleteTemplateAsAdminAsync(int templateId);
        Task<bool> RestoreTemplateAsync(int templateId);
        Task<bool> DeleteResponseAsAdminAsync(int responseId);
        Task<(IEnumerable<FormResponse> Responses, int TotalCount)> GetAllResponsesAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null);
        Task<(IEnumerable<ClerkUserDbModel> Users, int TotalCount)> GetUsersAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null);
            
        // Add new methods for user management
        Task<bool> SetUserAdminRoleAsync(string userId, bool isAdmin);
        Task<bool> BlockUserAsync(string userId, bool blocked);
        Task<bool> DeleteUserAsync(string userId);
        
        // Add diagnostic method
        Task<string> GetDatabaseDiagnosticsAsync();
        
        // Add Clerk user sync method
        Task<bool> SyncUsersFromClerkAsync();
    }
}