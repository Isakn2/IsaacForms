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
        return await _context.Templates
            .Where(t => EF.Functions.Contains(t.Title, query) ||
                        EF.Functions.Contains(t.Description, query))
            .ToListAsync();
    }
}