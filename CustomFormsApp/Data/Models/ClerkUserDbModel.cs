using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CustomFormsApp.Data.Models;

public class ClerkUserDbModel
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(100)] // Added MaxLength
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    [MaxLength(100)]
    public string? DisplayName { get; set; }
    
    // Add properties for admin functionality
    public bool IsAdmin { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public DateTime? BlockedAt { get; set; }
    public string? BlockedReason { get; set; }

    // Navigation properties
    public virtual ICollection<Template> CreatedTemplates { get; set; } = new List<Template>();
    public virtual ICollection<Form> CreatedForms { get; set; } = new List<Form>();
    public virtual ICollection<FormResponse> FormResponses { get; set; } = new List<FormResponse>();
    public virtual ICollection<TemplateAccess> TemplateAccesses { get; set; } = new List<TemplateAccess>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}