using System.ComponentModel.DataAnnotations;

namespace CustomFormsApp.Data.Models
{
    public class Topic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}