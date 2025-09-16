using System.Data.Entity;
using Plagiat.Models;

namespace Plagiat.Data
{
    public class PlagiatContext : DbContext
    {
        public PlagiatContext() : base("DefaultConnection")
        {
            // Создаем базу данных только если она не существует
            // При изменении модели база данных будет пересоздана
            Database.SetInitializer(new CreateDatabaseIfNotExists<PlagiatContext>());
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<PlagiarismResult> PlagiarismResults { get; set; }
        public DbSet<Citation> Citations { get; set; }
        public DbSet<Source> Sources { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Настройка связей
            modelBuilder.Entity<Document>()
                .HasMany(d => d.PlagiarismResults)
                .WithRequired(pr => pr.Document)
                .HasForeignKey(pr => pr.DocumentId);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.Citations)
                .WithRequired(c => c.Document)
                .HasForeignKey(c => c.DocumentId);

            modelBuilder.Entity<Citation>()
                .HasRequired(c => c.Source)
                .WithMany(s => s.Citations)
                .HasForeignKey(c => c.SourceId);

            // Связь Project -> Documents
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Documents)
                .WithOptional(d => d.Project)
                .HasForeignKey(d => d.ProjectId);

            // Настройка индексов
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.CreatedAt);

            modelBuilder.Entity<PlagiarismResult>()
                .HasIndex(pr => pr.SimilarityPercentage);

            // Настройка ограничений
            modelBuilder.Entity<Document>()
                .Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<Document>()
                .Property(d => d.Content)
                .IsOptional();

            modelBuilder.Entity<Source>()
                .Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(1000);

            modelBuilder.Entity<Project>()
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            base.OnModelCreating(modelBuilder);
        }
    }
}
