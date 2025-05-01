using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using CustomFormsApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class FormResponseService : IFormResponseService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IFormService _formService;
        private readonly ILogger<FormResponseService> _logger;

        public FormResponseService(IDbContextFactory<ApplicationDbContext> contextFactory, IFormService formService, ILogger<FormResponseService> logger)
        {
            _contextFactory = contextFactory;
            _formService = formService;
            _logger = logger;
        }

        public async Task<FormResponse> SubmitResponseAsync(int templateId, string userId, Dictionary<int, string> answers)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("SubmitResponseAsync called with null or empty userId for TemplateId {TemplateId}.", templateId);
                throw new UnauthorizedAccessException("User must be authenticated to submit a response.");
            }

            if (answers == null)
            {
                 _logger.LogWarning("SubmitResponseAsync called with null answers dictionary for TemplateId {TemplateId} by User {UserId}.", templateId, userId);
                 throw new ArgumentNullException(nameof(answers), "Answers dictionary cannot be null.");
            }

            // Use IFormService to get/create the Form associated with the Template
            Form? form = null; // Declare form here, nullable
            try
            {
                // Try up to 3 times to get the form, with exponential backoff
                int retryCount = 0;
                const int maxRetries = 3;
                Exception? lastException = null;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        // FIXED: Use GetOrCreateFormByTemplateIdAsync instead of GetOrCreateFormForTemplateAsync
                        // This properly handles the case where we have a template ID, not a form ID
                        form = await _formService.GetOrCreateFormByTemplateIdAsync(templateId, userId);
                        lastException = null; // Reset last exception on success
                        break; // Success, exit the retry loop
                    }
                    catch (ApplicationException appEx) when (appEx.InnerException is System.Net.Sockets.SocketException)
                    {
                        lastException = appEx;
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            // Exponential backoff: wait longer between each retry
                            int delayMs = (int)Math.Pow(2, retryCount) * 500;
                            _logger.LogWarning("Network error on attempt {RetryCount} getting form for template {TemplateId}. Retrying in {DelayMs}ms", 
                                retryCount, templateId, delayMs);
                            await Task.Delay(delayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        // For non-socket exceptions, fail immediately
                        _logger.LogError(ex, "Error getting or creating Form for Template {TemplateId} during response submission by User {UserId}.", templateId, userId);
                        throw;
                    }
                } // End while loop

                // If we've exhausted all retries and still have an exception
                if (lastException != null)
                {
                    _logger.LogError(lastException, "Failed to get or create form after {MaxRetries} retries for Template {TemplateId}", maxRetries, templateId);
                    // Throw the specific exception indicating retry failure
                    throw new ApplicationException($"Unable to process your submission after {maxRetries} attempts due to network issues. Please try again later.", lastException);
                }

                // If the loop finished without success BUT also without setting lastException (shouldn't happen with current logic, but good check)
                // OR if GetOrCreateFormByTemplateIdAsync somehow returned null without throwing an exception.
                if (form == null)
                {
                     _logger.LogError("Form object is null after attempting retrieval for Template {TemplateId} by User {UserId}, but no exception was caught.", templateId, userId);
                     throw new InvalidOperationException("Failed to retrieve or create the necessary form data.");
                }

                // --- Form is guaranteed to be non-null here ---

            }
            catch (KeyNotFoundException knfex) // Catch if template doesn't exist
            {
                 _logger.LogError(knfex, "Template {TemplateId} not found when submitting response by User {UserId}.", templateId, userId);
                 throw; // Re-throw specific exception
            }
            // Catch other potential exceptions from GetOrCreateFormByTemplateIdAsync if not handled by retry logic
            catch (Exception ex) when (ex is not ApplicationException && ex is not KeyNotFoundException)
            {
                 _logger.LogError(ex, "Unexpected error getting or creating Form for Template {TemplateId} by User {UserId}.", templateId, userId);
                 throw new ApplicationException("An unexpected error occurred while preparing your form submission.", ex);
            }

            // --- Transaction block ---
            // Now 'form' is guaranteed non-null if we reach this point.
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Use the execution strategy correctly for PostgreSQL
            try
            {
                // Create an execution strategy function to properly handle the transaction
                return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () => 
                {
                    // Inside this block, we can use transactions safely with the execution strategy
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    try
                    {
                        // Create the main response record
                        var newResponse = new FormResponse
                        {
                            FormId = form.Id, // Use the non-null form's Id
                            SubmittedById = userId,
                            SubmissionDate = DateTime.UtcNow
                        };

                        await context.FormResponses.AddAsync(newResponse);
                        // Save here to get the ResponseId for the Answers
                        await context.SaveChangesAsync();

                        // Create Answer records for each provided answer
                        var answerEntities = answers.Select(ap =>
                            new Answer
                            {
                                ResponseId = newResponse.Id,
                                QuestionId = ap.Key,
                                Value = ap.Value ?? string.Empty
                            }).ToList();

                        if (answerEntities.Any())
                        {
                            await context.Answers.AddRangeAsync(answerEntities);
                            await context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully submitted response {ResponseId} for Form {FormId} (Template {TemplateId}) by User {UserId}", 
                            newResponse.Id, form.Id, templateId, userId);

                        return newResponse;
                    }
                    catch (DbUpdateException dbEx)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(dbEx, "Database update error saving response for Form {FormId} (Template {TemplateId}) by User {UserId}",
                            form.Id, templateId, userId);
                        throw new ApplicationException("Failed to save your submission to the database. Please try again later.", dbEx);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error saving response or answers for Form {FormId} (Template {TemplateId}) by User {UserId}", 
                            form.Id, templateId, userId);
                        throw new ApplicationException("An error occurred while processing your submission. Please try again later.", ex);
                    }
                });
            }
            catch (System.Net.Sockets.SocketException sockEx) // Catch socket exceptions during save
            {
                _logger.LogError(sockEx, "Network error saving response for Form {FormId} (Template {TemplateId}) by User {UserId}",
                    form.Id, templateId, userId);
                throw new ApplicationException("A network error occurred while saving your submission. Please try again later.", sockEx);
            }
            catch (Exception ex) when (ex is not ApplicationException)
            {
                _logger.LogError(ex, "Unexpected error saving response for Form {FormId} (Template {TemplateId}) by User {UserId}", 
                    form.Id, templateId, userId);
                throw new ApplicationException("An unexpected error occurred while processing your submission. Please try again later.", ex);
            }
        }

        public async Task<IEnumerable<FormResponse>> GetUserResponsesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GetUserResponsesAsync called with null or empty userId.");
                return Enumerable.Empty<FormResponse>();
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.FormResponses
                    .Where(r => r.SubmittedById == userId)
                    .Include(r => r.Form) // Include the Form
                        .ThenInclude(f => f!.Template) // Then include the Template from the Form
                    .OrderByDescending(r => r.SubmissionDate)
                    .AsNoTracking() // Read-only operation
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching responses for User {UserId}", userId);
                return Enumerable.Empty<FormResponse>(); // Return empty list on error
            }
        }
        // --- Implementation for GetResponsesForFormAsync ---
        public async Task<IEnumerable<FormResponse>> GetResponsesForFormAsync(int formId)
        {
            if (formId <= 0)
            {
                _logger.LogWarning("GetResponsesForFormAsync called with invalid formId {FormId}.", formId);
                return Enumerable.Empty<FormResponse>();
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.FormResponses
                    .Where(r => r.FormId == formId)
                    .Include(r => r.SubmittedBy) // Include user details
                    .Include(r => r.Answers) // Include the list of answers
                        .ThenInclude(a => a.Question) // For each answer, include the question text/details
                    .OrderByDescending(r => r.SubmissionDate)
                    .AsNoTracking() // Read-only operation
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching responses for Form {FormId}", formId);
                return Enumerable.Empty<FormResponse>(); // Return empty list on error
            }
        }
        public async Task<FormResponse?> GetResponseAsync(int responseId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.FormResponses
                    .Include(r => r.SubmittedBy)
                    .Include(r => r.Form)
                        .ThenInclude(f => f!.Template) // Include template info
                    .Include(r => r.Answers)
                        .ThenInclude(a => a.Question) // Include questions for answers
                    .FirstOrDefaultAsync(r => r.Id == responseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching response with ID {ResponseId}", responseId);
                return null;
            }
        }
    }
}