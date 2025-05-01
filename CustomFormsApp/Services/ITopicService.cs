using CustomFormsApp.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public interface ITopicService
    {
        Task<List<Topic>> GetTopicsAsync();
        // Add methods for Create/Update/Delete if admin management is needed later
    }
}