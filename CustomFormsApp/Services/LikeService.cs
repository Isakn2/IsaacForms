using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class LikeService : ILikeService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<LikeService> _logger;

        public LikeService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<LikeService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<(bool IsLiked, int LikeCount)> ToggleLikeAsync(int templateId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("ToggleLikeAsync called with null or empty userId for TemplateId {TemplateId}.", templateId);
                throw new UnauthorizedAccessException("User must be authenticated to like a template.");
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Check if template exists and is not deleted (using query filter implicitly)
                var templateExists = await context.Templates.AnyAsync(t => t.Id == templateId);
                if (!templateExists)
                {
                     _logger.LogWarning("ToggleLikeAsync called for non-existent or deleted TemplateId {TemplateId}.", templateId);
                     throw new KeyNotFoundException($"Template with ID {templateId} not found or has been deleted.");
                }

                var existingLike = await context.Likes
                    .FirstOrDefaultAsync(l => l.TemplateId == templateId && l.UserId == userId);

                bool isNowLiked;

                if (existingLike != null)
                {
                    // User has already liked it, so unlike it
                    context.Likes.Remove(existingLike);
                    _logger.LogInformation("User {UserId} unliked Template {TemplateId}", userId, templateId);
                    isNowLiked = false;
                }
                else
                {
                    // User has not liked it, so add a like
                    var newLike = new Like
                    {
                        TemplateId = templateId,
                        UserId = userId,
                        LikedDate = DateTime.UtcNow
                    };
                    await context.Likes.AddAsync(newLike);
                    _logger.LogInformation("User {UserId} liked Template {TemplateId}", userId, templateId);
                    isNowLiked = true;
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving changes while toggling like for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                    throw new ApplicationException("Failed to save like status. Please try again.", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error saving changes while toggling like for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                    throw new ApplicationException("An unexpected error occurred while processing your request.", ex);
                }

                // Get the updated count after saving changes
                int newLikeCount = await GetLikeCountAsync(templateId);

                return (isNowLiked, newLikeCount);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error toggling like for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                throw new ApplicationException("Database error occurred. Please try again later.", dbEx);
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                _logger.LogError(sockEx, "Network error toggling like for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                throw new ApplicationException("A network error occurred. Please try again later.", sockEx);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not KeyNotFoundException && ex is not ApplicationException)
            {
                _logger.LogError(ex, "Unhandled error in ToggleLikeAsync for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                throw new ApplicationException("An error occurred while processing your request. Please try again later.", ex);
            }
        }

        public async Task<bool> HasUserLikedAsync(int templateId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false; // Anonymous users can't have liked anything
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                try 
                {
                    // First check if template exists to avoid unnecessary DB queries for deleted templates
                    var templateExists = await context.Templates.AnyAsync(t => t.Id == templateId);
                    if (!templateExists)
                    {
                        _logger.LogWarning("HasUserLikedAsync called for non-existent Template {TemplateId}", templateId);
                        return false;
                    }
                    
                    // Check includes template existence via query filter on Like entity
                    return await context.Likes
                        .AnyAsync(l => l.TemplateId == templateId && l.UserId == userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking if user {UserId} liked template {TemplateId}", userId, templateId);
                    return false; // Fail safely by assuming not liked
                }
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                _logger.LogError(sockEx, "Network error in HasUserLikedAsync for Template {TemplateId} User {UserId}", templateId, userId);
                // Return false on connection error - defaults to unliked state
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in HasUserLikedAsync for Template {TemplateId} User {UserId}", templateId, userId);
                return false;
            }
        }

        public async Task<int> GetLikeCountAsync(int templateId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                try
                {
                    // First check if template exists to avoid unnecessary DB queries for deleted templates
                    var templateExists = await context.Templates.AnyAsync(t => t.Id == templateId);
                    if (!templateExists)
                    {
                        _logger.LogWarning("GetLikeCountAsync called for non-existent Template {TemplateId}", templateId);
                        return 0;
                    }
                    
                    // Check includes template existence via query filter on Like entity
                    return await context.Likes
                        .CountAsync(l => l.TemplateId == templateId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting like count for template {TemplateId}", templateId);
                    return 0; // Fail safely by returning 0 likes
                }
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                _logger.LogError(sockEx, "Network error in GetLikeCountAsync for Template {TemplateId}", templateId);
                // Return 0 on connection error
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetLikeCountAsync for Template {TemplateId}", templateId);
                return 0;
            }
        }
    }
}