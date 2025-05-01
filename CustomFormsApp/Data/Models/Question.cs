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
        
        public int? FormId { get; set; }
        public virtual Form? Form { get; set; }
        
        public int? TemplateId { get; set; }
        public virtual Template? Template { get; set; }
        
        public string? CreatedById { get; set; }
        public virtual ClerkUserDbModel? CreatedBy { get; set; }
        
        [MaxLength(100)]
        public string? Topic { get; set; }
        
        public int Order { get; set; }
        public List<string>? Options { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = null!;
        
        public QuestionType Type { get; set; }
        public bool IsRequired { get; set; }
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}