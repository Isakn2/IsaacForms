using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CustomFormsApp.Services;

public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TemplateService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Template> CreateTemplateAsync(Template template)
    {
        var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != null)
        {
            template.CreatorId = currentUserId; 
        }

        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<Template?> GetTemplateAsync(int id)
    {
        return await _context.Templates
            .Include(t => t.Creator)
            .Include(t => t.Questions)
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Template>> GetUserTemplatesAsync(string userId)
    {
        return await _context.Templates
            .Where(t => t.CreatorId == userId && !t.IsDeleted)  // Fixed typo here
            .Include(t => t.TemplateTags)
                .ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

}