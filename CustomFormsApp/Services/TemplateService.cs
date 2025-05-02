using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace CustomFormsApp.Services;

public class TemplateService : ITemplateService
{
    // Change to inject the factory
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ClerkAuthService _authService;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        // Inject the factory instead of the context directly
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IHttpContextAccessor httpContextAccessor,
        ClerkAuthService authService,
        ILogger<TemplateService> logger)
    {
        _contextFactory = contextFactory;
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _logger = logger;
    }

    // CreateTemplateAsync might become redundant if SaveTemplateAsync handles creation
    public async Task<Template> CreateTemplateAsync(Template template)
    {
        // Create context instance for this operation
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (_authService.IsAuthenticated && _authService.CurrentUser != null)
        {
            template.CreatedById = _authService.CurrentUser.Id;
            template.CreatedDate = DateTime.UtcNow;
            template.LastModifiedDate = DateTime.UtcNow;
        }
        else
        {
            throw new InvalidOperationException("User must be authenticated to create a template.");
        }

        context.Templates.Add(template);
        await context.SaveChangesAsync();
        return template;
    }

    public async Task<Template?> GetTemplateAsync(int id)
    {
        // Create context instance for this operation
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            _logger.LogInformation("Attempting to fetch template with ID {id}", id);
            
            // First fetch the template with user and tags
            var template = await context.Templates
                .Include(t => t.CreatedBy)
                .Include(t => t.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
                
            if (template == null)
            {
                _logger.LogWarning("Template with ID {id} not found or is marked as deleted", id);
                return null;
            }
            
            _logger.LogInformation("Template with ID {id} found: {title}", id, template.Title);
            
            // Fetch questions using a direct SQL query to ensure we get all questions
            var questions = await context.Questions
                .IgnoreQueryFilters() // Bypass query filters
                .Where(q => q.TemplateId == id)
                .AsNoTracking()
                .OrderBy(q => q.Order)
                .ToListAsync();
            
            _logger.LogInformation("Fetched {count} questions for template {id}", 
                questions.Count, id);
            
            // Log question details for debugging
            foreach (var question in questions)
            {
                _logger.LogDebug("Question {id}: Order={order}, Text={text}, Type={type}", 
                    question.Id, question.Order, question.Text, question.Type);
            }
            
            // Attach questions to the template
            template.Questions = questions;
            
            _logger.LogInformation("Retrieved template {id} with {count} questions", id, questions.Count);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {id}: {message}", id, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Template>> GetUserTemplatesAsync(string userId)
    {
        // Create context instance for this operation
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Templates
            .Where(t => t.CreatedById == userId && !t.IsDeleted)
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.LastModifiedDate)
            .ToListAsync();
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        // Create context instance for this operation
        await using var context = await _contextFactory.CreateDbContextAsync();
        var template = await context.Templates.FindAsync(templateId);

        if (template == null)
        {
            throw new KeyNotFoundException($"Template with ID {templateId} not found.");
        }

        var currentUserId = _authService.CurrentUser?.Id;
        if (template.CreatedById != currentUserId)
        {
            throw new UnauthorizedAccessException("User is not authorized to delete this template.");
        }

        template.IsDeleted = true;
        template.LastModifiedDate = DateTime.UtcNow;
        context.Templates.Update(template);
        await context.SaveChangesAsync();
    }

    public async Task<Template> SaveTemplateAsync(Template templateData, List<Question> questionsData)
    {
        _logger.LogInformation("SaveTemplateAsync starting for template {TemplateId} with {QuestionCount} questions", 
            templateData.Id, questionsData.Count);
                
        var currentUserId = _authService.CurrentUser?.Id;

        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("Template save failed: User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        _logger.LogInformation("Current user: {UserId}", currentUserId);

        // Use a single context for the entire operation
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            Template? templateToSave;

            if (templateData.Id > 0) // Existing Template
            {
                _logger.LogInformation("Updating existing template with ID: {TemplateId}", templateData.Id);
                templateToSave = await context.Templates
                                        .FirstOrDefaultAsync(t => t.Id == templateData.Id);

                if (templateToSave == null)
                {
                    _logger.LogWarning("Template not found with ID: {TemplateId}", templateData.Id);
                    throw new KeyNotFoundException("Template not found.");
                }
                if (templateToSave.CreatedById != currentUserId)
                {
                    _logger.LogWarning("User {UserId} cannot modify template {TemplateId} created by {CreatorId}", 
                        currentUserId, templateData.Id, templateToSave.CreatedById);
                    throw new UnauthorizedAccessException("User cannot modify this template.");
                }

                // Update properties
                templateToSave.Title = templateData.Title;
                templateToSave.Description = templateData.Description;
                templateToSave.IsPublic = templateData.IsPublic;
                templateToSave.Topic = templateData.Topic;
                templateToSave.LastModifiedDate = DateTime.UtcNow;

                context.Templates.Update(templateToSave);
                _logger.LogInformation("Updated template properties for ID: {TemplateId}", templateToSave.Id);
            }
            else // New Template
            {
                _logger.LogInformation("Creating new template");
                templateToSave = new Template
                {
                    Title = templateData.Title,
                    Description = templateData.Description,
                    IsPublic = templateData.IsPublic,
                    Topic = templateData.Topic,
                    CreatedById = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    ImageUrl = templateData.ImageUrl,
                    ImageBlobName = templateData.ImageBlobName
                };

                await context.Templates.AddAsync(templateToSave);
                await context.SaveChangesAsync(); // Save to get the ID
                _logger.LogInformation("Created new template with ID: {TemplateId}", templateToSave.Id);
            }

            // --- Manage Questions ---
            _logger.LogInformation("Processing {count} questions for template {id}", 
                questionsData.Count, templateToSave.Id);

            // Fetch existing questions using IgnoreQueryFilters to see all questions regardless of filter state
            var existingQuestions = await context.Questions
                .IgnoreQueryFilters()
                .Where(q => q.TemplateId == templateToSave.Id && q.FormId == null)
                .ToListAsync();
            
            _logger.LogInformation("Found {ExistingCount} existing questions in database for template {TemplateId}", 
                existingQuestions.Count, templateToSave.Id);

            // First, delete questions not in the incoming data
            var incomingQuestionIds = questionsData.Where(q => q.Id > 0).Select(q => q.Id).ToHashSet();
            _logger.LogInformation("Received {IncomingCount} questions with existing IDs", incomingQuestionIds.Count);

            // Delete questions from database that are not in the incoming data
            if (existingQuestions.Any())
            {
                var questionsToDelete = existingQuestions.Where(eq => !incomingQuestionIds.Contains(eq.Id)).ToList();
                
                if (questionsToDelete.Any())
                {
                    _logger.LogInformation("Deleting {Count} questions that were removed", questionsToDelete.Count);
                    
                    // Remove through EF Core
                    context.Questions.RemoveRange(questionsToDelete);
                    
                    foreach (var question in questionsToDelete)
                    {
                        _logger.LogInformation("Marked question ID: {id} for deletion", question.Id);
                    }
                }
            }

            // Create a dictionary of existing questions by ID for easy lookup
            var existingQuestionsDict = existingQuestions.ToDictionary(q => q.Id);
            
            // Track statistics
            int updatedCount = 0;
            int newCount = 0;
            
            // Process each incoming question
            foreach (var questionData in questionsData)
            {
                // Ensure question is linked to this template
                questionData.TemplateId = templateToSave.Id;
                questionData.FormId = null; // Not a form question
                
                if (questionData.Id > 0 && existingQuestionsDict.TryGetValue(questionData.Id, out var existingQuestion))
                {
                    // Update existing question through EF Core
                    existingQuestion.Text = questionData.Text;
                    existingQuestion.Type = questionData.Type;
                    existingQuestion.Order = questionData.Order;
                    existingQuestion.IsRequired = questionData.IsRequired;
                    existingQuestion.Description = questionData.Description;
                    existingQuestion.Topic = questionData.Topic;
                    
                    // Explicitly handle the Options collection
                    if (questionData.Options != null)
                    {
                        // Create a new list to avoid any reference issues
                        existingQuestion.Options = new List<string>(questionData.Options);
                        _logger.LogInformation("Updated question ID {id} with {count} options", 
                            questionData.Id, questionData.Options.Count);
                    }
                    else
                    {
                        existingQuestion.Options = null;
                    }
                    
                    context.Questions.Update(existingQuestion);
                    updatedCount++;
                    
                    _logger.LogInformation("Updated existing question ID {id}", questionData.Id);
                }
                else
                {
                    // This is a new question
                    var newQuestion = new Question
                    {
                        Text = questionData.Text,
                        Type = questionData.Type,
                        Order = questionData.Order,
                        IsRequired = questionData.IsRequired,
                        Description = questionData.Description,
                        Topic = questionData.Topic,
                        TemplateId = templateToSave.Id,
                        FormId = null,
                        CreatedById = currentUserId
                    };
                    
                    // Explicitly handle the Options collection to ensure it's properly copied
                    if (questionData.Options != null)
                    {
                        newQuestion.Options = new List<string>(questionData.Options);
                        _logger.LogInformation("New question created with {count} options", 
                            questionData.Options.Count);
                    }
                    
                    await context.Questions.AddAsync(newQuestion);
                    newCount++;
                    
                    _logger.LogInformation("Added new question");
                }
            }
            
            _logger.LogInformation("Updated {UpdatedCount} existing questions and added {NewCount} new questions", 
                updatedCount, newCount);

            // Save all changes in a single operation
            await context.SaveChangesAsync();
            _logger.LogInformation("SaveChanges completed successfully");
            
            // Fetch the complete template with questions
            var result = await GetTemplateAsync(templateToSave.Id);
            
            if (result == null)
            {
                _logger.LogWarning("Could not fetch saved template {TemplateId}", templateToSave.Id);
                // If we can't fetch it, return the saved template
                return templateToSave;
            }
            
            _logger.LogInformation("Successfully saved and retrieved Template {TemplateId} with {QuestionCount} questions", 
                result.Id, result.Questions?.Count ?? 0);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving template {TemplateId}: {ErrorMessage}", 
                templateData.Id, ex.Message);
            throw; // Rethrow the original exception
        }
    }

    public async Task<IEnumerable<Template>> GetLatestPublicTemplatesAsync(int count)
    {
        try
        {
            // Create context instance for this operation
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Templates
                .Where(t => t.IsPublic && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedDate)
                .Take(count)
                .Include(t => t.CreatedBy)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest public templates.");
            return Enumerable.Empty<Template>();
        }
    }

    public async Task<IEnumerable<Template>> GetPopularPublicTemplatesAsync(int count)
    {
        try
        {
             // Create context instance for this operation
            await using var context = await _contextFactory.CreateDbContextAsync();
            // Note: Including Responses might be inefficient for popularity if there are many.
            // Consider adding a 'LikeCount' or 'ResponseCount' property to Template updated via triggers or background jobs.
            return await context.Templates
                .Where(t => t.IsPublic && !t.IsDeleted)
                .OrderByDescending(t => 
                    context.Forms.Count(f => f.TemplateId == t.Id)) // Order by form count as a measure of popularity
                .Take(count)
                .Include(t => t.CreatedBy)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular public templates.");
            return Enumerable.Empty<Template>();
        }
    }

    public async Task CleanupDuplicateQuestionsAsync(int templateId)
    {
        _logger.LogInformation("Starting cleanup of duplicate questions for template {TemplateId}", templateId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            // Get all questions for this template
            var allQuestions = await context.Questions
                .Where(q => q.TemplateId == templateId)
                .OrderBy(q => q.Order)
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} total questions for template {TemplateId}", 
                allQuestions.Count, templateId);
                
            // Group by Order value (assuming questions with same Order are duplicates)
            var questionGroups = allQuestions.GroupBy(q => q.Order).ToList();
            
            int removedCount = 0;
            
            foreach (var group in questionGroups)
            {
                var questions = group.ToList();
                
                // If we have duplicates for this order position
                if (questions.Count > 1)
                {
                    _logger.LogInformation("Found {Count} duplicate questions at order position {Order}", 
                        questions.Count, group.Key);
                    
                    // Keep the most recently added question (highest ID) and remove others
                    var keepQuestion = questions.OrderByDescending(q => q.Id).First();
                    var removeQuestions = questions.Where(q => q.Id != keepQuestion.Id).ToList();
                    
                    _logger.LogInformation("Keeping question ID {KeepId}, removing {RemoveCount} duplicates", 
                        keepQuestion.Id, removeQuestions.Count);
                        
                    context.Questions.RemoveRange(removeQuestions);
                    removedCount += removeQuestions.Count;
                }
            }
            
            if (removedCount > 0)
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Successfully removed {Count} duplicate questions for template {TemplateId}", 
                    removedCount, templateId);
            }
            else
            {
                _logger.LogInformation("No duplicate questions found for template {TemplateId}", templateId);
                await transaction.RollbackAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up duplicate questions for template {TemplateId}", templateId);
            await transaction.RollbackAsync();
            throw;
        }
    }
}