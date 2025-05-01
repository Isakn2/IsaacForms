using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;

        public CommentService(ApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Comment>> GetCommentsForTemplateAsync(int templateId)
        {
            try
            {
                // The HasQueryFilter on Comment entity handles the IsDeleted check for the template
                return await _context.Comments
                    .Where(c => c.TemplateId == templateId)
                    .Include(c => c.User) // Include user information for display
                    .OrderByDescending(c => c.CreatedDate) // Show newest comments first
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for TemplateId {TemplateId}", templateId);
                return new List<Comment>(); // Return empty list on error
            }
        }

        public async Task<Comment> AddCommentAsync(int templateId, string userId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("AddCommentAsync called with null or empty userId for TemplateId {TemplateId}.", templateId);
                throw new UnauthorizedAccessException("User must be authenticated to post a comment.");
            }

            if (string.IsNullOrWhiteSpace(commentText))
            {
                 _logger.LogWarning("AddCommentAsync called with empty comment text for TemplateId {TemplateId} by User {UserId}.", templateId, userId);
                 throw new ArgumentException("Comment text cannot be empty.", nameof(commentText));
            }

            // Check if template exists and is not deleted (using query filter implicitly)
            var templateExists = await _context.Templates.AnyAsync(t => t.Id == templateId);
            if (!templateExists)
            {
                 _logger.LogWarning("AddCommentAsync called for non-existent or deleted TemplateId {TemplateId}.", templateId);
                 throw new KeyNotFoundException($"Template with ID {templateId} not found or has been deleted.");
            }

            var newComment = new Comment
            {
                TemplateId = templateId,
                UserId = userId,
                Text = commentText.Trim(), // Trim whitespace
                CreatedDate = DateTime.UtcNow
            };

            try
            {
                await _context.Comments.AddAsync(newComment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} added comment to Template {TemplateId}", userId, templateId);

                // Eager load the User navigation property for the returned object
                await _context.Entry(newComment).Reference(c => c.User).LoadAsync();

                return newComment;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving new comment for TemplateId {TemplateId} by User {UserId}", templateId, userId);
                throw; // Re-throw for caller to handle
            }
        }
    }
}