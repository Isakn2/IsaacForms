using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Services;

public interface ISearchService
{
    Task<IEnumerable<Template>> SearchTemplatesAsync(string query);
}