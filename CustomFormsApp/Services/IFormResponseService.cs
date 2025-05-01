using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public interface IFormResponseService
    {
        Task<FormResponse> SubmitResponseAsync(int templateId, string userId, Dictionary<int, string> answers);
        Task<IEnumerable<FormResponse>> GetUserResponsesAsync(string userId);
        Task<IEnumerable<FormResponse>> GetResponsesForFormAsync(int formId);
        Task<FormResponse?> GetResponseAsync(int responseId);
    }
}