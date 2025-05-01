// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using CustomFormsApp.Data.Models;

namespace CustomFormsApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ClerkUserDbModel> Users { get; set; } = null!;
        public DbSet<TemplateAccess> TemplateAccesses { get; set; } = null!;
        public DbSet<Template> Templates { get; set; } = null!;
        public DbSet<Form> Forms { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<FormResponse> FormResponses { get; set; } = null!;
        public DbSet<Answer> Answers { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TemplateTag> TemplateTags { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;
        public DbSet<Topic> Topics { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ClerkUserDbModel
            builder.Entity<ClerkUserDbModel>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedNever(); // ID comes from Clerk

                entity.Property(u => u.FirstName)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(u => u.LastName)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(u => u.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(u => u.Username)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(u => u.ImageUrl)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique().HasFilter("\"Username\" IS NOT NULL"); // Unique constraint only if Username is not null
                entity.HasMany(u => u.CreatedTemplates)
                    .WithOne(t => t.CreatedBy)
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasMany(u => u.CreatedForms)
                    .WithOne(f => f.CreatedBy)
                    .HasForeignKey(f => f.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasMany(u => u.FormResponses)
                    .WithOne(r => r.SubmittedBy)
                    .HasForeignKey(r => r.SubmittedById)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasMany(u => u.Comments)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.Likes)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict); 

                 entity.HasMany(u => u.TemplateAccesses)
                    .WithOne(ta => ta.User)
                    .HasForeignKey(ta => ta.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Template
            builder.Entity<Template>(entity =>
            {
                entity.Property(t => t.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(t => t.Description)
                    .HasMaxLength(4000)
                    .IsRequired(false); // Allow null description

                entity.Property(t => t.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(t => t.ImageBlobName)
                    .HasMaxLength(200);

                entity.Property(t => t.Topic)
                    .HasMaxLength(100);

                entity.HasIndex(t => t.CreatedDate);
                entity.HasIndex(t => t.IsPublic);
                entity.HasIndex(t => t.IsDeleted);
                entity.HasIndex(t => new { t.Title, t.Description }); // Example composite index for search

                // Relationships for Template
                entity.HasMany(t => t.Questions)
                    .WithOne(q => q.Template)
                    .HasForeignKey(q => q.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting template deletes its questions

                entity.HasMany(t => t.TemplateTags)
                    .WithOne(tt => tt.Template)
                    .HasForeignKey(tt => tt.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting template removes tag associations

                entity.HasMany(t => t.Comments)
                    .WithOne(c => c.Template)
                    .HasForeignKey(c => c.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting template deletes comments

                entity.HasMany(t => t.Likes)
                    .WithOne(l => l.Template)
                    .HasForeignKey(l => l.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting template deletes likes

                entity.HasMany(t => t.RestrictedAccess)
                    .WithOne(ta => ta.Template)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting template removes access rights

                // Query Filter for soft delete
                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // Configure Form
            builder.Entity<Form>(entity =>
            {
                entity.Property(f => f.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                // Relationships for Form
                entity.HasMany(f => f.Questions)
                    .WithOne(q => q.Form)
                    .HasForeignKey(q => q.FormId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting form deletes its questions

                entity.HasMany(f => f.Responses)
                    .WithOne(r => r.Form)
                    .HasForeignKey(r => r.FormId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting form deletes its responses

                entity.HasOne(f => f.Template)
                    .WithMany() // No navigation property back from Template to Forms
                    .HasForeignKey(f => f.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent deleting template if forms exist

                // Query Filter to exclude forms whose template is soft-deleted
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

                entity.Property(q => q.Type)
                    .HasMaxLength(20) // e.g., "Text", "MultipleChoice"
                    .IsRequired();

                entity.Property(q => q.Options) // Store options as JSON string or similar
                    .IsRequired(false);

                entity.HasIndex(q => q.Order);
                entity.HasQueryFilter(q => q.Form != null && (q.Form.TemplateId == null || (q.Form.Template != null && !q.Form.Template.IsDeleted)));


                // Relationships for Question
                entity.HasMany(q => q.Answers)
                    .WithOne(a => a.Question)
                    .HasForeignKey(a => a.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict); // 
            });

            // Configure FormResponse
            builder.Entity<FormResponse>(entity =>
            {
                entity.HasIndex(r => r.SubmissionDate);
                entity.HasQueryFilter(r => r.Form != null && (r.Form.TemplateId == null || (r.Form.Template != null && !r.Form.Template.IsDeleted)));


                // Relationships for FormResponse
                entity.HasMany(r => r.Answers)
                    .WithOne(a => a.Response)
                    .HasForeignKey(a => a.ResponseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Answer
            builder.Entity<Answer>(entity =>
            {
                entity.Property(a => a.Value)
                    .HasMaxLength(4000) 
                    .IsRequired();
                entity.HasQueryFilter(a => a.Question != null && a.Question.Form != null && (a.Question.Form.TemplateId == null || (a.Question.Form.Template != null && !a.Question.Form.Template.IsDeleted)));

            });

            // Configure Tag
            builder.Entity<Tag>(entity =>
            {
                entity.Property(t => t.Name)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(t => t.Name).IsUnique();

                // Relationships for Tag
                entity.HasMany(t => t.TemplateTags)
                    .WithOne(tt => tt.Tag)
                    .HasForeignKey(tt => tt.TagId)
                    .OnDelete(DeleteBehavior.Cascade); // Deleting tag removes associations
            });

            // Configure TemplateTag (Join Table)
            builder.Entity<TemplateTag>(entity =>
            {
                entity.HasKey(tt => new { tt.TemplateId, tt.TagId });
                entity.HasQueryFilter(tt => tt.Template != null && !tt.Template.IsDeleted);

                // Relationships configured in Template and Tag entities
            });

            // Configure Comment
            builder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.Text)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasIndex(c => c.CreatedDate);
                entity.HasQueryFilter(c => c.Template != null && !c.Template.IsDeleted);

                // Relationships configured in User and Template entities
            });

            // Configure Like
            builder.Entity<Like>(entity =>
            {
                entity.HasKey(l => new { l.UserId, l.TemplateId }); // Composite key
                entity.HasQueryFilter(l => l.Template != null && !l.Template.IsDeleted);

            });

             // Configure TemplateAccess
            builder.Entity<TemplateAccess>(entity =>
            {
                entity.HasKey(ta => new { ta.TemplateId, ta.UserId }); // Composite key
                entity.HasQueryFilter(ta => ta.Template != null && !ta.Template.IsDeleted);

                // Relationships configured in User and Template entities
            });

            builder.Entity<Topic>().HasData(
                new Topic { Id = 1, Name = "General" },
                new Topic { Id = 2, Name = "Education" },
                new Topic { Id = 3, Name = "Business" },
                new Topic { Id = 4, Name = "Feedback" },
                new Topic { Id = 5, Name = "Events" }
            );
        }
    }
}