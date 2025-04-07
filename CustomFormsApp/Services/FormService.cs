// FormService.cs
using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomFormsApp.Services;

public class FormService : IFormService
{
    private readonly ApplicationDbContext _context;

    public FormService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Form> CreateFormAsync(Form form)
    {
        if (form == null)
        {
            throw new ArgumentNullException(nameof(form));
        }

        form.Id = 0; 
        form.CreatedDate = DateTime.UtcNow;

        _context.Forms.Add(form);
        await _context.SaveChangesAsync();
        return form;
    }

    public async Task<FormResponse> SubmitFormAsync(FormResponse formResponse)
    {
        if (formResponse == null)
        {
            throw new ArgumentNullException(nameof(formResponse));
        }

        formResponse.SubmittedDate = DateTime.UtcNow;
        _context.FormResponses.Add(formResponse);
        await _context.SaveChangesAsync();
        return formResponse;
    }

    public async Task<Form?> GetFormAsync(int formId) // Updated to match interface
    {
        return await _context.Forms
            .Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == formId);
    }

    public async Task<FormResponse?> GetFormResponseAsync(int id) // Updated to match interface
    {
        return await _context.FormResponses
            .Include(fr => fr.Answers)
            .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(fr => fr.Id == id);
    }

    public async Task<List<Form>> GetUserFormsAsync(string userId)
    {
        return await _context.Forms
            .Where(f => f.CreatorId == userId)
            .ToListAsync();
    }

    public async Task DeleteFormAsync(string formId) 
    {
        var form = await _context.Forms.FindAsync(formId);
        if (form != null)
        {
            _context.Forms.Remove(form);
            await _context.SaveChangesAsync();
        }
    }
}