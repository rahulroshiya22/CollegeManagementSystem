using CMS.FeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.FeeService.Data
{
    public class FeeDbContext : DbContext
    {
        public FeeDbContext(DbContextOptions<FeeDbContext> options) : base(options) { }
        
        public DbSet<Fee> Fees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Fee>(entity =>
            {
                entity.HasKey(e => e.FeeId);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            });
        }
    }
}
