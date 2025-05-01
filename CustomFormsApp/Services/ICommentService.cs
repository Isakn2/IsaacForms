using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public interface ICommentService
    {
        /// <summary>
        /// Gets all comments for a specific template, ordered by date.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>A list of comments for the template.</returns>
        Task<List<Comment>> GetCommentsForTemplateAsync(int templateId);

        /// <summary>
        /// Adds a new comment to a template.
        /// </summary>
        /// <param name="templateId">The ID of the template to comment on.</param>
        /// <param name="userId">The ID of the user posting the comment.</param>
        /// <param name="commentText">The text content of the comment.</param>
        /// <returns>The newly created comment.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if userId is null or empty.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the template does not exist or is deleted.</exception>
        /// <exception cref="ArgumentException">Thrown if commentText is null or whitespace.</exception>
        Task<Comment> AddCommentAsync(int templateId, string userId, string commentText);

        // Optional: Add methods for deleting or editing comments later if needed
        // Task DeleteCommentAsync(int commentId, string userId);
        // Task<Comment> UpdateCommentAsync(int commentId, string userId, string newText);
    }
}