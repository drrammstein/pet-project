using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
    }
    public DbSet<AppData> AppData { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppData>().HasData(
            new AppData { Id = 1, Key = "main_value", Value = "Initial value from database", UpdatedAt = DateTime.UtcNow }
            );
    }
}
