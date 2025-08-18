using ApiPreProcessamento.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPreProcessamento.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Script> Scripts => Set<Script>();
        public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    }
}
