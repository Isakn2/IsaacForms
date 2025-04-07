// Data/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Make CreatorId nullable or provide a default
        public string? CreatorId { get; set; }
        
        // Self-referencing relationship
        public virtual ApplicationUser? Creator { get; set; }
        
        // Collection navigation for users created by this user
        public virtual ICollection<ApplicationUser> CreatedUsers { get; set; } = new List<ApplicationUser>();
        
        // Other properties
        public string? DisplayName { get; set; }
        
        // Navigation collections
        public virtual ICollection<Template> CreatedTemplates { get; set; } = new List<Template>();
        public virtual ICollection<Form> CreatedForms { get; set; } = new List<Form>();
        public virtual ICollection<FormResponse> FormResponses { get; set; } = new List<FormResponse>();
        public virtual ICollection<TemplateAccess> TemplateAccesses { get; set; } = new List<TemplateAccess>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}