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
            : base(options)
        {
        }

        public DbSet<TemplateAccess> TemplateAccesses { get; set; }
        public DbSet<Template> Templates { get; set; }
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
                entity.Property(u => u.DisplayName)
                    .HasMaxLength(100);
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
                    
                entity.HasIndex(t => t.CreatedDate);
                entity.HasIndex(t => t.IsPublic);
                
                // Full-text search configuration
                entity.HasIndex(t => new { t.Title, t.Description })
                    .IsClustered(false);
            });

            // Configure Question
            builder.Entity<Question>(entity =>
            {
                entity.Property(q => q.Text)
                    .HasMaxLength(1000)
                    .IsRequired();
                    
                entity.HasIndex(q => q.Order);
                
                entity.HasOne(q => q.Template)
                    .WithMany(t => t.Questions)
                    .HasForeignKey(q => q.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure FormResponse
            builder.Entity<FormResponse>(entity =>
            {
                entity.HasIndex(fr => fr.SubmittedDate);
                
                entity.HasOne(fr => fr.Template)
                    .WithMany(t => t.Responses)
                    .HasForeignKey(fr => fr.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(fr => fr.Respondent)
                    .WithMany(u => u.FormResponses)
                    .HasForeignKey(fr => fr.RespondentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Answer
            builder.Entity<Answer>(entity =>
            {
                entity.Property(a => a.Value)
                    .HasMaxLength(4000);
                    
                entity.HasOne(a => a.Question)
                    .WithMany()
                    .HasForeignKey(a => a.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(a => a.Response)
                    .WithMany(r => r.Answers)
                    .HasForeignKey(a => a.ResponseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Tag
            builder.Entity<Tag>(entity =>
            {
                entity.Property(t => t.Name)
                    .HasMaxLength(50)
                    .IsRequired();
                    
                entity.HasIndex(t => t.Name)
                    .IsUnique();
            });

            // Configure TemplateTag (junction table)
            builder.Entity<TemplateTag>()
                .HasKey(tt => new { tt.TemplateId, tt.TagId });

            // Configure Comment
            builder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.Content)
                    .HasMaxLength(2000)
                    .IsRequired();
                    
                entity.HasIndex(c => c.CreatedDate);
                
                entity.HasOne(c => c.Template)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(c => c.Author)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<FormResponse>()
                .HasIndex(fr => fr.TemplateId);

            builder.Entity<Answer>()
                .HasIndex(a => a.QuestionId);

            builder.Entity<Comment>()
                .HasIndex(c => c.TemplateId);

            // Configure Like
            builder.Entity<Like>()
                .HasKey(l => new { l.TemplateId, l.UserId });
                
            // Configure TemplateAccess
            builder.Entity<TemplateAccess>()
                .HasKey(ta => new { ta.TemplateId, ta.UserId });

            // Only enable soft delete if models have IsDeleted property
            builder.Entity<Template>().HasQueryFilter(t => !t.IsDeleted);
            builder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);
        }
    }
}