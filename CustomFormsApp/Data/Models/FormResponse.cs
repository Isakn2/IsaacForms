// FormResponse.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomFormsApp.Data.Models
{
    public class FormResponse
    {
        public int Id { get; set; }

        // Foreign Key for Form
        [Required]
        public int FormId { get; set; } 

        // Navigation property for Form
        [ForeignKey("FormId")]
        public virtual Form? Form { get; set; } 

        // Foreign Key for SubmittedBy User
        [Required]
        public string SubmittedById { get; set; } = string.Empty; 

        // Navigation property for SubmittedBy User
        [ForeignKey("SubmittedById")]
        public virtual ClerkUserDbModel? SubmittedBy { get; set; } 

        // Submission Date
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow; 

        // Navigation properties
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}