// Like.cs
namespace CustomFormsApp.Data.Models
{
    public class Like
    {
        public int TemplateId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime LikedDate { get; set; } = DateTime.UtcNow;
        
        public Template Template { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
