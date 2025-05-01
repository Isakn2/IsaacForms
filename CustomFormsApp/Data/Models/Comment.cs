using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomFormsApp.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }

        // Foreign Key for User
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation property for User
        [ForeignKey("UserId")]
        public virtual ClerkUserDbModel? User { get; set; }

        // Foreign Key for Template
        [Required]
        public int? TemplateId { get; set; }

        // Navigation property for Template
        [ForeignKey("TemplateId")]
        public virtual Template? Template { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty; // Added Text property

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}