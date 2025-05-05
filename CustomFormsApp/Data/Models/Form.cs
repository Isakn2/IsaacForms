using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace CustomFormsApp.Data.Models
{
    public class Form
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Foreign Key for CreatedBy
        [Required]
        public string CreatedById { get; set; } = string.Empty; 
        
        // Navigation property for CreatedBy
        [ForeignKey("CreatedById")]
        public virtual ClerkUserDbModel? CreatedBy { get; set; } 
        
        // Foreign Key for Template (optional)
        public int? TemplateId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property for Template
        [ForeignKey("TemplateId")]
        public virtual Template? Template { get; set; }

        // Navigation properties
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

        public virtual ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>(); 

        [NotMapped]
        public string? Topic { get; set; }

        // Property for making forms visible to all users
        public bool IsPublic { get; set; }
    }
}