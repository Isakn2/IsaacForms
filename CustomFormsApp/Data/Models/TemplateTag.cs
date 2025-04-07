// TemplateTag.cs
namespace CustomFormsApp.Data.Models
{
    public class TemplateTag
    {
        public int TemplateId { get; set; } // Changed from string to int
        public Template Template { get; set; } = null!;

        public int TagId { get; set; } // Changed from string to int
        public Tag Tag { get; set; } = null!;
    }
}