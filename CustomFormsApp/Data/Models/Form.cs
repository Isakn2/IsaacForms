using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CustomFormsApp.Data.Models
{
    public class Form
    {
        public int Id { get; set; }
        public string CreatorId { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        public Template? Template { get; set; }
        public int? TemplateId { get; set; }
        
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}