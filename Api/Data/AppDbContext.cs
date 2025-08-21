using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Script> Scripts => Set<Script>();
        public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índice para acelerar busca de jobs pendentes
            modelBuilder.Entity<ProcessingJob>()
                            .HasIndex(j => new { j.Status, j.CreatedAt });
        }
    }
}
