// FormService.cs
using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomFormsApp.Services;

public class FormService : IFormService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ClerkAuthService _authService;
    private readonly ILogger<FormService> _logger;

    public FormService(IDbContextFactory<ApplicationDbContext> contextFactory, ClerkAuthService authService, ILogger<FormService> logger)
    {
        _contextFactory = contextFactory;
        _authService = authService;
        _logger = logger;
    }

    public async Task<Form> CreateFormAsync(Form form)
    {
        if (form == null)
        {
            throw new ArgumentNullException(nameof(form));
        }

        form.Id = 0; 
        form.CreatedDate = DateTime.UtcNow;

        using var context = await _contextFactory.CreateDbContextAsync();
        context.Forms.Add(form);
        await context.SaveChangesAsync();
        return form;
    }

    public async Task<FormResponse> SubmitFormAsync(FormResponse formResponse)
    {
        if (formResponse == null)
        {
            throw new ArgumentNullException(nameof(formResponse));
        }

        formResponse.SubmissionDate = DateTime.UtcNow;
        using var context = await _contextFactory.CreateDbContextAsync();
        context.FormResponses.Add(formResponse);
        await context.SaveChangesAsync();
        return formResponse;
    }

    public async Task<Form?> GetFormAsync(int formId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Forms
            .Include(f => f.CreatedBy)
            .Include(f => f.Questions.OrderBy(q => q.Order))
            .Include(f => f.Template) // Include template info if needed
            .FirstOrDefaultAsync(f => f.Id == formId);
    }

    public async Task<FormResponse?> GetFormResponseAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FormResponses
            .Include(fr => fr.Answers)
            .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(fr => fr.Id == id);
    }

    public async Task<IEnumerable<Form>> GetUserFormsAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Forms
            .Where(f => f.CreatedById == userId)
            .Include(f => f.Template) // Include related data if needed
            .OrderByDescending(f => f.CreatedDate)
            .ToListAsync(); // ToListAsync() is compatible with IEnumerable<Form>
    }

    public async Task<IEnumerable<Form>> GetPublicFormsAsync(int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        try {
            // Verify each form's template exists if the form has a template ID
            var validForms = await context.Forms
                .Where(f => f.IsPublic)
                .Include(f => f.CreatedBy)
                .Include(f => f.Template) // Include template to verify it exists
                .Where(f => !f.TemplateId.HasValue || (f.TemplateId.HasValue && f.Template != null && !f.Template.IsDeleted))
                .OrderByDescending(f => f.CreatedDate)
                .Take(count)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} valid public forms", validForms.Count);
            return validForms;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error retrieving public forms");
            return new List<Form>(); // Return empty list on error
        }
    }

    public async Task DeleteFormAsync(int formId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Implementation needed if not already present
        var form = await context.Forms.FindAsync(formId);
        if (form == null) throw new KeyNotFoundException($"Form with ID {formId} not found.");

        // Optional: Add ownership check here using _authService or passed userId

        context.Forms.Remove(form); // Cascade delete should handle Questions and Responses
        await context.SaveChangesAsync();
        _logger.LogInformation("Deleted Form {FormId}", formId);
    }

    public async Task<Form> SaveFormAsync(Form form, List<Question> questions)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Implementation needed if not already present
        // Similar logic to TemplateService.SaveTemplateAsync but for Forms
        // Ensure FormId is set on questions, TemplateId should be null
        _logger.LogWarning("SaveFormAsync is not fully implemented.");
        await Task.Delay(10); // Placeholder
        return form; // Placeholder
    }

    // --- Implementation for GetOrCreateFormForTemplateAsync ---
    public async Task<Form> GetOrCreateFormForTemplateAsync(int formId, string userIdForCreation)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            // Validate the userIdForCreation
            if (string.IsNullOrEmpty(userIdForCreation))
            {
                throw new ArgumentException("A valid user ID is required to create a form template", nameof(userIdForCreation));
            }
            
            // Check if the user exists in the database
            var userExists = await _context.Users.AnyAsync(u => u.Id == userIdForCreation);
            if (!userExists)
            {
                _logger.LogError("Cannot create template: User ID {UserId} does not exist in the database", userIdForCreation);
                throw new InvalidOperationException($"User ID {userIdForCreation} not found. Cannot create a template.");
            }

            // Try to get the form - with a more optimized query that limits the data retrieved
            var form = await _context.Forms
                .Include(f => f.Questions.OrderBy(q => q.Order))
                .AsNoTracking() // For read-only operations, this can improve performance
                .FirstOrDefaultAsync(f => f.Id == formId);
                
            if (form == null)
            {
                throw new KeyNotFoundException($"Form with ID {formId} not found");
            }
            
            // Check if the form already has a TemplateId and the template exists
            if (form.TemplateId.HasValue && form.TemplateId.Value > 0)
            {
                // Verify if the template exists - using a simple query to avoid additional joins
                var templateExists = await _context.Templates
                    .Where(t => t.Id == form.TemplateId.Value && !t.IsDeleted)
                    .AnyAsync();
                
                if (templateExists)
                {
                    return form; // Form already has a valid template associated with it
                }
                // If template doesn't exist, we'll create a new one
            }
            
            // Create a template based on this form for form filling
            var template = new Template
            {
                Title = form.Name,
                Description = form.Description,
                CreatedById = userIdForCreation, // Using the validated user ID
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                IsPublic = form.IsPublic,
                Topic = "" // Ensure this is not null
            };
            
            _context.Templates.Add(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new template {TemplateId} for form {FormId}", template.Id, formId);
            
            // Add questions from form to template
            if (form.Questions != null && form.Questions.Any())
            {
                // Batch question creation to minimize database roundtrips
                var templateQuestions = form.Questions.Select(question => new Question
                {
                    Text = question.Text,
                    Type = question.Type,
                    Order = question.Order,
                    IsRequired = question.IsRequired,
                    Options = question.Options,
                    Description = question.Description,
                    TemplateId = template.Id,
                    CreatedById = userIdForCreation
                }).ToList();
                
                _context.Questions.AddRange(templateQuestions);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Added {QuestionCount} questions to template {TemplateId}", form.Questions.Count, template.Id);
            }
            
            // Link the form to the template
            form.TemplateId = template.Id;
            _context.Forms.Update(form);
            await _context.SaveChangesAsync();
            
            return form;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update error when creating form template for Form {FormId} User {UserId}", 
                formId, userIdForCreation);
            throw new ApplicationException($"Failed to save changes to the database for form {formId}.", dbEx);
        }
        catch (System.Net.Sockets.SocketException sockEx)
        {
            _logger.LogError(sockEx, "Socket error when accessing the database for Form {FormId} User {UserId}", 
                formId, userIdForCreation);
            throw new ApplicationException("A network error occurred while communicating with the database. Please try again later.", sockEx);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error when creating form template for Form {FormId} User {UserId}", 
                formId, userIdForCreation);
            throw new ApplicationException("An unexpected error occurred while processing your request. Please try again later.", ex);
        }
    }

    // New method to find or create a form for a template when filling out forms
    public async Task<Form> GetOrCreateFormByTemplateIdAsync(int templateId, string userIdForCreation)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(userIdForCreation))
            {
                throw new ArgumentException("A valid user ID is required", nameof(userIdForCreation));
            }
            
            // First check if the template exists
            var template = await context.Templates
                .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);
                
            if (template == null)
            {
                throw new KeyNotFoundException($"Template with ID {templateId} not found or has been deleted");
            }
            
            // Look for any existing form that uses this template
            var existingForm = await context.Forms
                .FirstOrDefaultAsync(f => f.TemplateId == templateId);
                
            if (existingForm != null)
            {
                _logger.LogInformation("Found existing form {FormId} for template {TemplateId}", 
                    existingForm.Id, templateId);
                return existingForm;
            }
            
            // Create a new form based on this template
            var newForm = new Form
            {
                Name = template.Title,
                Description = template.Description,
                TemplateId = templateId,
                CreatedById = userIdForCreation,
                CreatedDate = DateTime.UtcNow,
                IsPublic = template.IsPublic
            };
            
            context.Forms.Add(newForm);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Created new form {FormId} for template {TemplateId}", 
                newForm.Id, templateId);
                
            return newForm;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error creating form for template {TemplateId}", templateId);
            throw new ApplicationException($"Failed to save changes to the database for template {templateId}.", dbEx);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error creating form for template {TemplateId}", templateId);
            throw new ApplicationException("An unexpected error occurred while processing your request.", ex);
        }
    }

    // Implementation of the missing method
    public async Task<IEnumerable<Form>> GetFormsByUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Forms
            .Where(f => f.CreatedById == userId)
            .Include(f => f.Template)
            .Include(f => f.Questions.OrderBy(q => q.Order))
            .OrderByDescending(f => f.CreatedDate)
            .ToListAsync();
    }
}