using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services;

public interface ITemplateService
{
    Task<Template> CreateTemplateAsync(Template template);
    Task<Template?> GetTemplateAsync(int id); 
    Task<IEnumerable<Template>> GetUserTemplatesAsync(string userId);
}