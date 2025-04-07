namespace CustomFormsApp.Data.Enums
{
    public enum QuestionType
    {
        SingleLineText,
        MultiLineText,
        Number,  // Changed from Integer to Number for consistency
        Checkbox
    }

    public static class QuestionTypeExtensions
    {
        public static string ToDisplayString(this QuestionType type)
        {
            return type switch
            {
                QuestionType.SingleLineText => "Single Line Text",
                QuestionType.MultiLineText => "Multi Line Text",
                QuestionType.Number => "Number",
                QuestionType.Checkbox => "Checkbox",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}