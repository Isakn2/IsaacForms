// TemplateAccess.cs
namespace CustomFormsApp.Data.Models
{
    public class TemplateAccess
    {
        public int TemplateId { get; set; }
        public string UserId { get; set; } = null!;
        
        public Template Template { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}