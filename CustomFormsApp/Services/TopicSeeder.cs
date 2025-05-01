using CustomFormsApp.Data;
using CustomFormsApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomFormsApp.Services
{
    public class TopicSeeder
    {
        public static async Task SeedTopicsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            
            // Check if Topics table exists and has no records
            if (!await dbContext.Topics.AnyAsync())
            {
                Console.WriteLine("No topics found in database. Seeding default topics...");
                
                var defaultTopics = new List<Topic>
                {
                    new Topic { Name = "General" },
                    new Topic { Name = "Education" },
                    new Topic { Name = "Business" },
                    new Topic { Name = "Feedback" },
                    new Topic { Name = "Events" },
                    new Topic { Name = "Survey" },
                    new Topic { Name = "Registration" },
                    new Topic { Name = "Customer" },
                    new Topic { Name = "Product" },
                    new Topic { Name = "Marketing" }
                };
                
                await dbContext.Topics.AddRangeAsync(defaultTopics);
                await dbContext.SaveChangesAsync();
                
                Console.WriteLine($"Successfully seeded {defaultTopics.Count} topics to the database.");
            }
            else
            {
                Console.WriteLine("Topics table already has records. Skipping seeding.");
            }
        }
    }
}