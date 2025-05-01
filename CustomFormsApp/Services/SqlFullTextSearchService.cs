using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services;

public class SqlFullTextSearchService : IFullTextSearchService
{
    private readonly ApplicationDbContext _context;

    public SqlFullTextSearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    // PostgreSQL-compatible search using ILIKE and full-text search
    public async Task<IEnumerable<Template>> SearchTemplatesAsync(string query, bool includeDeleted = false)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        // Use ILIKE for simple search, or use full-text search if you want more advanced matching
        var templates = _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.Questions)
            .AsNoTracking()
            .Where(t => (includeDeleted || !t.IsDeleted) &&
                (
                    EF.Functions.ILike(t.Title, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%")) ||
                    t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, $"%{query}%"))
                ))
            .OrderByDescending(t => t.CreatedDate);

        return await templates.ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchTemplatesByTagAsync(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => !t.IsDeleted && t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, tag)))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchPublicTemplatesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.IsPublic && !t.IsDeleted &&
                (
                    EF.Functions.ILike(t.Title, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%")) ||
                    t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, $"%{query}%"))
                ))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchAllTemplatesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => !t.IsDeleted &&
                (
                    EF.Functions.ILike(t.Title, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%")) ||
                    t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, $"%{query}%"))
                ))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchUserTemplatesAsync(string query, string userId)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.CreatedById == userId && !t.IsDeleted &&
                (
                    EF.Functions.ILike(t.Title, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%")) ||
                    t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, $"%{query}%"))
                ))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchLikedTemplatesAsync(string query, string userId)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        return await _context.Likes
            .Where(l => l.UserId == userId)
            .Select(l => l.Template)
            .Where(t => t != null && !t.IsDeleted &&
                (
                    EF.Functions.ILike(t.Title, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%")) ||
                    t.TemplateTags.Any(tt => EF.Functions.ILike(tt.Tag.Name, $"%{query}%"))
                ))
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchTemplatesByTopicAsync(string topicName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Include(t => t.CreatedBy)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => !t.IsDeleted && 
                   (t.Topic != null && EF.Functions.ILike(t.Topic, $"%{topicName}%")))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }
}