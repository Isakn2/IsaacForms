using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services;

public interface IFullTextSearchService
{
    Task<IEnumerable<Template>> SearchTemplatesAsync(string query, bool includeDeleted = false);
    Task<IEnumerable<Template>> SearchPublicTemplatesAsync(string query);
    Task<IEnumerable<Template>> SearchUserTemplatesAsync(string query, string userId);
    Task<IEnumerable<Template>> SearchTemplatesByTagAsync(string tagName);
}