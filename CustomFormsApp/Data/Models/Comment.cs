// Comment.cs
namespace CustomFormsApp.Data.Models
{
    public class Comment
    {
        public int? Id { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int TemplateId { get; set; }
        public string CreatorId { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public Template Template { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;
        public DateTime? DeletedDate { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}