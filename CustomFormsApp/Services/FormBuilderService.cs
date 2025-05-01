using CustomFormsApp.Data;
using CustomFormsApp.Data.Enums;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class FormBuilderService : IFormBuilderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormBuilderService> _logger;
        private readonly ICurrentUserService _currentUserService; // Add this
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private Dictionary<Question, string> _questionClientIds = new Dictionary<Question, string>();

        public FormBuilderService(
            ApplicationDbContext context,
            ILogger<FormBuilderService> logger,
            ICurrentUserService currentUserService,
            IDbContextFactory<ApplicationDbContext> contextFactory
        )
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _contextFactory = contextFactory;
        }

        // Keep existing method implementations
        public async Task<List<Question>> GetQuestionsAsync(int formId)
        {
            // Existing implementation
            try
            {
                return await _context.Questions
                    .Where(q => q.FormId == formId)
                    .OrderBy(q => q.Order)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for form {FormId}", formId);
                throw;
            }
        }

        public async Task<Form?> GetCurrentFormAsync(int? formId = null)
        {
            try
            {
                if (formId.HasValue)
                {
                    return await _context.Forms
                        .Include(f => f.Questions)
                        .FirstOrDefaultAsync(f => f.Id == formId.Value);
                }

                // Or implement logic to get current user's draft form
                return await _context.Forms
                    .OrderByDescending(f => f.CreatedDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current form");
                throw;
            }
        }

        public async Task AddQuestionAsync(int formId, Question question)
        {
            try
            {
                question.Id = 0; // Default int value
                question.FormId = formId;
                question.Order = await _context.Questions
                    .Where(q => q.FormId == formId)
                    .CountAsync() + 1;

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question to form {FormId}", formId);
                throw;
            }
        }

        public async Task UpdateQuestionOrderAsync(int formId, List<int> orderedIds)
        {
            try
            {
                var questions = await _context.Questions
                    .Where(q => q.FormId == formId)
                    .ToListAsync();

                for (int i = 0; i < orderedIds.Count; i++)
                {
                    var question = questions.FirstOrDefault(q => q.Id == orderedIds[i]);
                    if (question != null)
                    {
                        question.Order = i + 1;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question order for form {FormId}", formId);
                throw;
            }
        }

        public async Task DeleteQuestionAsync(int questionId)
        {
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question != null)
                {
                    _context.Questions.Remove(question);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                throw;
            }
        }

        public async Task<int> SaveFormAsync(Form form, List<Question> questions)
        {
            // Use execution strategy and new DbContext for retries
            var strategy = _contextFactory.CreateDbContext().Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Use fresh context instance per attempt
                await using var context = _contextFactory.CreateDbContext();
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var currentUserId = _currentUserService.GetUserId();
                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        throw new InvalidOperationException("User is not authenticated. Cannot save form.");
                    }
                    
                    Form? existingForm = null;
                    
                    if (form.Id > 0) 
                    {
                        // Get existing form with tracking
                        existingForm = await context.Forms.FindAsync(form.Id);
                        
                        if (existingForm == null)
                        {
                            throw new KeyNotFoundException($"Form with ID {form.Id} not found.");
                        }
                        
                        if (existingForm.CreatedById != currentUserId)
                        {
                            throw new UnauthorizedAccessException("You don't have permission to update this form.");
                        }
                        
                        // Update form properties
                        existingForm.Name = form.Name;
                        existingForm.Description = form.Description;
                        existingForm.IsPublic = form.IsPublic;
                        existingForm.UpdatedDate = DateTime.UtcNow;
                        
                        context.Forms.Update(existingForm);
                    }
                    else
                    {
                        // Creating new form
                        form.CreatedDate = DateTime.UtcNow;
                        form.UpdatedDate = DateTime.UtcNow;
                        form.CreatedById = currentUserId;
                        
                        context.Forms.Add(form);
                    }

                    // Save to get the form ID if it's new
                    await context.SaveChangesAsync();
                    
                    int formId = existingForm?.Id ?? form.Id;
                    
                    // Get existing questions to detect deleted ones
                    var existingQuestions = await context.Questions
                        .Where(q => q.FormId == formId)
                        .ToListAsync();
                    
                    var updatedQuestionIds = questions
                        .Where(q => q.Id > 0)
                        .Select(q => q.Id)
                        .ToHashSet();
                    
                    // Delete questions that are no longer in the list
                    var questionsToDelete = existingQuestions
                        .Where(q => !updatedQuestionIds.Contains(q.Id))
                        .ToList();
                    
                    if (questionsToDelete.Any())
                    {
                        context.Questions.RemoveRange(questionsToDelete);
                        _logger.LogInformation("Deleting {Count} questions for Form {FormId}", 
                            questionsToDelete.Count, formId);
                    }
                    
                    // Update existing questions or add new ones
                    foreach (var question in questions)
                    {
                        question.FormId = formId;
                        
                        if (question.Id > 0)
                        {
                            // Update existing question
                            var existingQuestion = existingQuestions.FirstOrDefault(q => q.Id == question.Id);
                            
                            if (existingQuestion != null)
                            {
                                // Update properties manually
                                existingQuestion.Text = question.Text;
                                existingQuestion.Type = question.Type;
                                existingQuestion.Order = question.Order;
                                existingQuestion.IsRequired = question.IsRequired;
                                existingQuestion.Options = question.Options;
                                
                                context.Questions.Update(existingQuestion);
                            }
                        }
                        else
                        {
                            // New question
                            question.CreatedById = currentUserId;
                            context.Questions.Add(question);
                        }
                    }
                    
                    // Save changes
                    await context.SaveChangesAsync();
                    
                    // Commit the transaction
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Successfully saved Form {FormId}", formId);
                    
                    return formId;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error saving form: {ErrorMessage}", ex.Message);
                    throw;
                }
            });
        }
        
        public string GetQuestionClientId(Question question, int index)
        {
            if (question.Id > 0)
            {
                return $"q-{question.Id}";
            }
            
            if (_questionClientIds.TryGetValue(question, out var clientId))
            {
                return clientId;
            }
            
            clientId = $"new-{DateTime.Now.Ticks}-{index}";
            _questionClientIds[question] = clientId;
            return clientId;
        }
    }

    public interface IFormBuilderService 
    {
        Task<List<Question>> GetQuestionsAsync(int formId);
        Task<Form?> GetCurrentFormAsync(int? formId = null);
        Task AddQuestionAsync(int formId, Question question);
        Task UpdateQuestionOrderAsync(int formId, List<int> orderedIds);
        Task DeleteQuestionAsync(int questionId);
        Task<int> SaveFormAsync(Form form, List<Question> questions);
    }
}