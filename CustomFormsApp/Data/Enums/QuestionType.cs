namespace CustomFormsApp.Data.Enums
{
    public enum QuestionType
    {
        SingleLineText,
        MultiLineText,
        MultipleChoice,
        Checkbox,
        Date,
        Number,
        Email,
        Scale,
        Dropdown
    }

    public static class QuestionTypeExtensions
    {
        public static string ToDisplayString(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleLineText => "Short Text",
                QuestionType.MultiLineText => "Paragraph Text",
                QuestionType.MultipleChoice => "Multiple Choice",
                QuestionType.Checkbox => "Checkboxes",
                QuestionType.Date => "Date",
                QuestionType.Number => "Number",
                QuestionType.Email => "Email",
                QuestionType.Scale => "Rating Scale",
                QuestionType.Dropdown => "Dropdown",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        
        public static bool SupportsOptions(this QuestionType type)
        {
            return type switch
            {
                QuestionType.MultipleChoice => true,
                QuestionType.Checkbox => true,
                QuestionType.Dropdown => true,
                QuestionType.Scale => true,
                _ => false
            };
        }
        
        public static bool RequiresOptions(this QuestionType type)
        {
            return type switch
            {
                QuestionType.MultipleChoice => true,
                QuestionType.Checkbox => true,
                QuestionType.Dropdown => true,
                _ => false
            };
        }
        
        public static string GetPlaceholder(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleLineText => "Short text response",
                QuestionType.MultiLineText => "Longer, multi-line response",
                QuestionType.Number => "Numeric value (e.g. 42)",
                QuestionType.Email => "Email address (e.g. user@example.com)",
                QuestionType.Date => "Date selection",
                QuestionType.Scale => "Rating from 1-5",
                _ => string.Empty
            };
        }
        
        public static string GetIconClass(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleLineText => "bi-input-cursor-text",
                QuestionType.MultiLineText => "bi-textarea-t",
                QuestionType.MultipleChoice => "bi-ui-radios",
                QuestionType.Checkbox => "bi-check-square",
                QuestionType.Date => "bi-calendar",
                QuestionType.Number => "bi-123",
                QuestionType.Email => "bi-envelope",
                QuestionType.Scale => "bi-stars",
                QuestionType.Dropdown => "bi-menu-button-wide",
                _ => "bi-question-circle"
            };
        }
    }
}