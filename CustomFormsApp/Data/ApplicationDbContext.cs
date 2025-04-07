// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CustomFormsApp.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CustomFormsApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TemplateAccess> TemplateAccesses { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Form> Forms { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<FormResponse> FormResponses { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TemplateTag> TemplateTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                // Add this configuration for self-reference
                entity.HasOne(u => u.Creator)
                    .WithMany(u => u.CreatedUsers)
                    .HasForeignKey(u => u.CreatorId)
                    .IsRequired(false)  // Make it optional
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationships
                entity.HasMany(u => u.CreatedTemplates)
                    .WithOne(t => t.Creator)
                    .HasForeignKey(t => t.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);  

                entity.HasMany(u => u.CreatedForms)
                    .WithOne(f => f.Creator)
                    .HasForeignKey(f => f.CreatorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.TemplateAccesses)
                    .WithOne(ta => ta.User)
                    .HasForeignKey(ta => ta.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Comments)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.Likes)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed to Restrict

                entity.HasMany(u => u.FormResponses)
                    .WithOne(fr => fr.User)
                    .HasForeignKey(fr => fr.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Property configurations
                entity.Property(u => u.DisplayName)
                    .HasMaxLength(100)
                    .IsRequired(false);  // Optional

                entity.Property(u => u.UserName)
                    .HasMaxLength(256);

                entity.Property(u => u.NormalizedUserName)
                    .HasMaxLength(256);

                entity.Property(u => u.Email)
                    .HasMaxLength(256);

                entity.Property(u => u.NormalizedEmail)
                    .HasMaxLength(256);

                // Indexes
                entity.HasIndex(u => u.NormalizedEmail)
                    .HasDatabaseName("IX_User_Email")
                    .IsUnique();

                entity.HasIndex(u => u.NormalizedUserName)
                    .HasDatabaseName("IX_User_Username")
                    .IsUnique();

                entity.HasIndex(u => u.DisplayName)
                    .HasDatabaseName("IX_User_DisplayName")
                    .IsUnique(false);

                entity.HasIndex(u => u.CreatorId)
                    .HasDatabaseName("IX_User_CreatorId");
            });
            
            // Configure Template
            builder.Entity<Template>(entity =>
            {
                entity.Property(t => t.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(t => t.Description)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.Property(t => t.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(t => t.ImageBlobName)
                    .HasMaxLength(200);

                entity.Property(t => t.Topic)
                    .HasMaxLength(100);

                entity.HasIndex(t => t.CreatedDate);
                entity.HasIndex(t => t.IsPublic);
                entity.HasIndex(t => t.IsDeleted);
                entity.HasIndex(t => new { t.Title, t.Description });

                entity.HasMany(t => t.Questions)
                    .WithOne(q => q.Template)
                    .HasForeignKey(q => q.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(t => t.Responses)
                    .WithOne(r => r.Template)
                    .HasForeignKey(r => r.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(t => t.Comments)
                    .WithOne(c => c.Template)
                    .HasForeignKey(c => c.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(t => t.Likes)
                    .WithOne(l => l.Template)
                    .HasForeignKey(l => l.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict

                entity.HasMany(t => t.RestrictedAccess)
                    .WithOne(ta => ta.Template)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict

                entity.HasMany(t => t.Tags)
                    .WithMany(t => t.Templates)
                    .UsingEntity<TemplateTag>(
                        tt => tt.HasOne(e => e.Tag).WithMany(t => t.TemplateTags),
                        tt => tt.HasOne(e => e.Template).WithMany(t => t.TemplateTags)
                    );

                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // Configure Form
            builder.Entity<Form>(entity =>
            {
                entity.Property(f => f.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasMany(f => f.Questions)
                    .WithOne(q => q.Form)
                    .HasForeignKey(q => q.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Template)
                    .WithMany()
                    .HasForeignKey(f => f.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(f => (f.TemplateId == null) || (!f.Template!.IsDeleted));
            });

            // Configure Question
            builder.Entity<Question>(entity =>
            {
                entity.Property(q => q.Text)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(q => q.Description)
                    .HasMaxLength(2000);

                entity.HasIndex(q => q.Order);

                entity.Property(q => q.Type)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                
                entity.HasQueryFilter(q => q.Template != null && !q.Template.IsDeleted);
            });

            // Configure Tag
            builder.Entity<Tag>(entity =>
            {
                entity.Property(t => t.Name)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(t => t.Name)
                    .IsUnique();

                entity.HasMany(t => t.TemplateTags)
                    .WithOne(tt => tt.Tag)
                    .HasForeignKey(tt => tt.TagId);
            });

            // Configure FormResponse
            builder.Entity<FormResponse>(entity =>
            {
                entity.HasIndex(r => r.SubmittedDate);

                entity.HasMany(r => r.Answers)
                    .WithOne(a => a.Response)
                    .HasForeignKey(a => a.ResponseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(r => !r.Template.IsDeleted);
            });

            // Configure Answer
            builder.Entity<Answer>(entity =>
            {
                entity.Property(a => a.Value)
                    .HasMaxLength(4000);

                entity.HasQueryFilter(a => a.Question != null && a.Question.Template != null && !a.Question.Template.IsDeleted);
            });

            // Configure Comment
            builder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.Content)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasIndex(c => c.CreatedDate);
                entity.HasIndex(c => c.IsDeleted);

                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // Configure Like
            builder.Entity<Like>(entity =>
            {
                entity.HasKey(l => new { l.TemplateId, l.UserId });
                entity.HasQueryFilter(l => !l.Template.IsDeleted);
            });

            // Configure TemplateAccess
            builder.Entity<TemplateAccess>(entity =>
            {
                // Composite primary key
                entity.HasKey(ta => new { ta.TemplateId, ta.UserId });
                
                // Query filter
                entity.HasQueryFilter(ta => !ta.Template.IsDeleted);
                
                // Relationship with Template
                entity.HasOne(ta => ta.Template)
                    .WithMany(t => t.RestrictedAccess)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict
                
                // Relationship with User (if you have this navigation property)
                entity.HasOne(ta => ta.User)
                    .WithMany(u => u.TemplateAccesses)
                    .HasForeignKey(ta => ta.UserId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict
            });

            // Configure TemplateTag
            builder.Entity<TemplateTag>(entity =>
            {
                // Composite primary key
                entity.HasKey(tt => new { tt.TemplateId, tt.TagId });
                
                // Query filter
                entity.HasQueryFilter(tt => !tt.Template.IsDeleted);
                
                // Relationship with Template
                entity.HasOne(tt => tt.Template)
                    .WithMany(t => t.TemplateTags)
                    .HasForeignKey(tt => tt.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict
                
                // Relationship with Tag
                entity.HasOne(tt => tt.Tag)
                    .WithMany(t => t.TemplateTags)
                    .HasForeignKey(tt => tt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}