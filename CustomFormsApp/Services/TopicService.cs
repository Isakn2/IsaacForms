using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomFormsApp.Services
{
    public class TopicService : ITopicService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TopicService> _logger;

        public TopicService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TopicService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Topic>> GetTopicsAsync()
        {
            try
            {
                // Create a new database context for this operation
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                // Return topics ordered by name
                return await context.Topics
                                   .OrderBy(t => t.Name)
                                   .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching topics from database: {Message}", ex.Message);
                return new List<Topic>(); // Return empty list on error
            }
        }
    }
}