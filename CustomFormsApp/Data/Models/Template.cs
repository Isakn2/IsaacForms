// Template.cs
using System.Collections.Generic;

namespace CustomFormsApp.Data.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!; // Markdown content
        public string? ImageBlobName { get; set; } // Azure Blob Storage name
        public string? ImageUrl { get; set; } // Public URL
        public bool IsPublic { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedDate { get; set; }

        // Relationships
        public ApplicationUser Creator { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
        public ICollection<TemplateAccess> RestrictedAccess { get; set; } = new List<TemplateAccess>();
    }
}