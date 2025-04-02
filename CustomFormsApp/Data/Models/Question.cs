// Question.cs
using CustomFormsApp.Data.Enums;

namespace CustomFormsApp.Data.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int Order { get; set; }
        public string Text { get; set; } = null!;
        public QuestionType Type { get; set; } // Enum: Text, MultiLineText, Number, Checkbox
        public bool IsRequired { get; set; }
        
        public Template Template { get; set; } = null!;
    }
}