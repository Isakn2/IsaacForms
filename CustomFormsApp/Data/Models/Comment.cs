// Comment.cs
namespace CustomFormsApp.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int TemplateId { get; set; }
        public string AuthorId { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public Template Template { get; set; } = null!;
        public ApplicationUser Author { get; set; } = null!;
        public DateTime? DeletedDate { get; set; }
    }
}