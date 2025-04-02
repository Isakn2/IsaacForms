// Tag.cs
namespace CustomFormsApp.Data.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
    }
}