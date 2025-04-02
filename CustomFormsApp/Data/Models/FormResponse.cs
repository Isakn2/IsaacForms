// FormResponse.cs
namespace CustomFormsApp.Data.Models
{
    public class FormResponse
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string RespondentId { get; set; } = null!;
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        
        public Template Template { get; set; } = null!;
        public ApplicationUser Respondent { get; set; } = null!;
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}