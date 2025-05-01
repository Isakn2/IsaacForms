using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace CustomFormsApp.Services;

public interface IFormService
{
    Task<Form> CreateFormAsync(Form form);
    Task<FormResponse> SubmitFormAsync(FormResponse formResponse);
    Task<FormResponse?> GetFormResponseAsync(int id);
    Task DeleteFormAsync(int formId);
    Task<Form?> GetFormAsync(int formId);
    Task<IEnumerable<Form>> GetUserFormsAsync(string userId); // Keep this one
    Task<IEnumerable<Form>> GetFormsByUserAsync(string userId); // Add this missing method signature
    Task<IEnumerable<Form>> GetPublicFormsAsync(int count);
    Task<Form> SaveFormAsync(Form form, List<Question> questions);
    Task<Form> GetOrCreateFormForTemplateAsync(int templateId, string userIdForCreation); // Add this method
    Task<Form> GetOrCreateFormByTemplateIdAsync(int templateId, string userIdForCreation); // Add our new method
}