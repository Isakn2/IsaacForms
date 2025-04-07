// Data/Models/Template.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq; // Added for LINQ operations
using CustomFormsApp.Data;

namespace CustomFormsApp.Data.Models
{
    public class Template
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(200)]
        public string? ImageBlobName { get; set; }

        [MaxLength(100)]
        public string? Topic { get; set; }

        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedDate { get; set; }

        public string CreatorId { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;

        // Relationships
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
        public virtual ICollection<TemplateAccess> RestrictedAccess { get; set; } = new List<TemplateAccess>();
        
        // Calculated properties
        [NotMapped]
        public IEnumerable<Tag> Tags => TemplateTags.Select(tt => tt.Tag);

        [NotMapped]
        public int ResponseCount => Responses?.Count ?? 0;

        [NotMapped]
        public int LikeCount => Likes?.Count ?? 0;
    }
}