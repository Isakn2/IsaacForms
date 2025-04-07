// Question.cs
using CustomFormsApp.Data.Enums;
using CustomFormsApp.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomFormsApp.Data.Models
{
    public class Question
    {
        public int Id { get; set; }
        
        public int FormId { get; set; }
        public Form Form { get; set; } = null!;
        
        public int? TemplateId { get; set; }  // Changed from string to int
        public Template? Template { get; set; }
        
        public string? CreatedById { get; set; }  // Optional if allowing null
        public virtual ApplicationUser? CreatedBy { get; set; }
        
        [MaxLength(100)]
        public string? Topic { get; set; }  // Made nullable
        
        public int Order { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = null!;
        
        public QuestionType Type { get; set; }
        public bool IsRequired { get; set; }
        
        [MaxLength(2000)]
        public string? Description { get; set; }  // Made nullable and removed null! marker
        
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}