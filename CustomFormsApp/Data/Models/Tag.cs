// Tag.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomFormsApp.Data.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        
        public string? Topic { get; set; }
        
        // Relationships
        public ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
        
        // Calculated property for easier navigation
        [NotMapped]
        public IEnumerable<Template> Templates => TemplateTags.Select(tt => tt.Template);
    }
}