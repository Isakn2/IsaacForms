// Data/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        
        // Navigation properties
        public ICollection<Template> CreatedTemplates { get; set; } = new List<Template>();
        public ICollection<FormResponse> FormResponses { get; set; } = new List<FormResponse>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}