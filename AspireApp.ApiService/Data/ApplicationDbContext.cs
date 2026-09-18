using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AspireApp.ApiService.Models;

namespace AspireApp.ApiService.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasMany(p => p.Tasks).WithOne(t => t.Project!).HasForeignKey(t => t.ProjectId);
        });

        builder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
        });
    }
}
