// Answer.cs
namespace CustomFormsApp.Data.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int ResponseId { get; set; } 
        public int QuestionId { get; set; } 
        public string Value { get; set; } = null!;
        
        public FormResponse Response { get; set; } = null!;
        public Question Question { get; set; } = null!;
    }
}