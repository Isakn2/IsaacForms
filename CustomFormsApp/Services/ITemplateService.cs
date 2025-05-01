using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services;

public interface ITemplateService
{
    Task<Template> CreateTemplateAsync(Template template);
    Task<Template?> GetTemplateAsync(int id);
    Task<IEnumerable<Template>> GetUserTemplatesAsync(string userId);
    Task DeleteTemplateAsync(int templateId);
    Task<Template> SaveTemplateAsync(Template template, List<Question> questions);
    Task<IEnumerable<Template>> GetLatestPublicTemplatesAsync(int count);
    Task<IEnumerable<Template>> GetPopularPublicTemplatesAsync(int count);
    Task CleanupDuplicateQuestionsAsync(int templateId);
}