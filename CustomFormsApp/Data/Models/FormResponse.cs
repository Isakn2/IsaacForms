// FormResponse.cs
namespace CustomFormsApp.Data.Models
{
    public class FormResponse
    {
        public int Id { get; set; } = 0;
        public int TemplateId { get; set; } // Changed from string to int
        public string UserId { get; set; } = null!; // Keep as string since ASP.NET Identity uses string for user IDs
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Template Template { get; set; } = null!;
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}