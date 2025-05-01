using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomFormsApp.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Template>> SearchTemplatesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Template>();

        return await _context.Templates
            .Where(t =>
                EF.Functions.ILike(t.Title, $"%{query}%") ||
                (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%"))
            )
            .ToListAsync();
    }
}