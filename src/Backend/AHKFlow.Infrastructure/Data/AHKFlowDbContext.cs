using AHKFlow.Domain.Entities;
using AHKFlow.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AHKFlow.Infrastructure.Data
{
    public class AHKFlowDbContext : DbContext
    {
        public AHKFlowDbContext(DbContextOptions<AHKFlowDbContext> options) : base(options)
        {
        }

        public DbSet<TestMessage> TestMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new TestMessageConfiguration());
        }
    }
}
