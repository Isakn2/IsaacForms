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
    public class FormTemplateService : IFormTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormTemplateService> _logger;

        public FormTemplateService(
            ApplicationDbContext context,
            ILogger<FormTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Question>> GetQuestionsAsync(int formId) // Updated to match interface
        {
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

        public async Task AddQuestionAsync(int formId, Question question) // Updated to match interface
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                if (form.Id == 0) 
                {
                    form.Id = 0; // Default int value
                    form.CreatedDate = DateTime.UtcNow;
                    form.UpdatedDate = DateTime.UtcNow;
                    _context.Forms.Add(form);
                }
                else
                {
                    form.UpdatedDate = DateTime.UtcNow;
                    _context.Forms.Update(form);
                }

                await _context.SaveChangesAsync();

                // Update all questions
                foreach (var question in questions)
                {
                    question.FormId = form.Id;
                    
                    if (question.Id == 0)
                    {
                        question.Id = 0; 
                        _context.Questions.Add(question);
                    }
                    else
                    {
                        _context.Questions.Update(question);
                    }
                }

                // Remove deleted questions
                var existingQuestionIds = questions.Select(q => q.Id).ToList();
                var questionsToRemove = await _context.Questions
                    .Where(q => q.FormId == form.Id && !existingQuestionIds.Contains(q.Id))
                    .ToListAsync();

                _context.Questions.RemoveRange(questionsToRemove);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return form.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving form {FormId}", form.Id);
                throw;
            }
        }
    }

    public interface IFormTemplateService
    {
        Task<List<Question>> GetQuestionsAsync(int formId);
        Task<Form?> GetCurrentFormAsync(int? formId = null);
        Task AddQuestionAsync(int formId, Question question);
        Task UpdateQuestionOrderAsync(int formId, List<int> orderedIds);
        Task DeleteQuestionAsync(int questionId);
        Task<int> SaveFormAsync(Form form, List<Question> questions);
    }
}