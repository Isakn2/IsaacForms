// TemplateTag.cs (junction table)
namespace CustomFormsApp.Data.Models
{
    public class TemplateTag
    {
        public int TemplateId { get; set; }
        public int TagId { get; set; }
        
        public Template Template { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}