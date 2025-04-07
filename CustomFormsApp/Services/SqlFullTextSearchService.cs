// SqlFullTextSearchService.cs
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

    public async Task<IEnumerable<Template>> SearchTemplatesAsync(string query, bool includeDeleted = false)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        // Use raw SQL with parameterization for safety
        FormattableString sql = $@"
            SELECT t.* FROM Templates t
            LEFT JOIN TemplateTags tt ON tt.TemplateId = t.Id
            LEFT JOIN Tags tag ON tag.Id = tt.TagId
            WHERE {(includeDeleted ? "" : "t.IsDeleted = 0 AND ")}
            (CONTAINS(t.Title, {query}) OR 
            CONTAINS(t.Description, {query}) OR
            CONTAINS(tag.Name, {query}))
            ORDER BY t.CreatedDate DESC";

        return await _context.Templates
            .FromSqlInterpolated(sql)
            .Include(t => t.Creator)
            .Include(t => t.TemplateTags)   
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.Questions)
            .AsNoTracking()
            .ToListAsync();

    }

    public async Task<IEnumerable<Template>> SearchPublicTemplatesAsync(string query)
    {
        return await _context.Templates
            .Include(t => t.Creator)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.IsPublic && !t.IsDeleted)
            .Where(t => EF.Functions.FreeText(t.Title, query) ||
                       EF.Functions.FreeText(t.Description, query) ||
                       t.TemplateTags.Any(tt => EF.Functions.FreeText(tt.Tag.Name, query)))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchUserTemplatesAsync(string query, string userId)
    {
        return await _context.Templates
            .Include(t => t.Creator)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.CreatorId.ToString() == userId && !t.IsDeleted)
            .Where(t => EF.Functions.FreeText(t.Title, query) ||
                       EF.Functions.FreeText(t.Description, query) ||
                       t.TemplateTags.Any(tt => EF.Functions.FreeText(tt.Tag.Name, query)))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Template>> SearchTemplatesByTagAsync(string tagName)
    {
        return await _context.Templates
            .Include(t => t.Creator)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => !t.IsDeleted && 
                       t.TemplateTags.Any(tt => tt.Tag.Name.Contains(tagName)))
            .OrderByDescending(t => t.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }
}