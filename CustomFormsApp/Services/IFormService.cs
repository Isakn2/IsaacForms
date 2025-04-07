using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Services;

public interface IFormService
{
    Task<Form> CreateFormAsync(Form form);
    Task<FormResponse> SubmitFormAsync(FormResponse formResponse);
    Task<Form?> GetFormAsync(int formId); 
    Task<FormResponse?> GetFormResponseAsync(int id); 
}